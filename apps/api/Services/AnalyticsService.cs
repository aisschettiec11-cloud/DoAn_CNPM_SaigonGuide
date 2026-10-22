using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Domain;

namespace SaigonGuide.Api.Services;

public class EventThrottle
{
    readonly Dictionary<Guid, (int Count, DateTimeOffset Until)> entries = [];
    readonly object sync = new();

    public bool Accept(Guid id)
    {
        lock (sync)
        {
            var now = DateTimeOffset.UtcNow;
            if (entries.Count > 10000)
                foreach (
                    var k in entries.Where(x => x.Value.Until < now).Select(x => x.Key).ToArray()
                )
                    entries.Remove(k);
            var v = entries.GetValueOrDefault(id);
            if (v.Until <= now)
                v = (0, now.AddMinutes(1));
            if (v.Count >= 120)
                return false;
            entries[id] = (v.Count + 1, v.Until);
            return true;
        }
    }
}

public class AnalyticsService(
    GuideDb db,
    IDataProtectionProvider protection,
    EventThrottle throttle
)
{
    const string VisitorCookie = "sg_visitor";
    readonly IDataProtector protector = protection.CreateProtector("saigon-guide.visitor.v1");

    public Guid? Visitor(HttpContext c)
    {
        try
        {
            if (
                c.Request.Cookies.TryGetValue(VisitorCookie, out var v)
                && v.Length < 2048
                && Guid.TryParse(protector.Unprotect(v), out var id)
            )
                return id;
        }
        catch (CryptographicException) { }
        return null;
    }

    async Task Lock(Guid visitor) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({BitConverter.ToInt64(visitor.ToByteArray(), 0)})"
        );

    public async Task<object> Session(bool consent, HttpContext c)
    {
        Rules.Require(consent, "Cần chủ động đồng ý Analytics.");
        Rules.Require(!c.User.IsInRole("Admin"), "Không ghi nhận hoạt động Admin.");
        var visitor = Visitor(c) ?? Guid.NewGuid();
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock(visitor);
        var now = DateTimeOffset.UtcNow;
        var s = await db.AnalyticsSessions.SingleOrDefaultAsync(x =>
            x.VisitorId == visitor && x.ClosedAt == null
        );
        if (s is not null && (s.LastEventAt ?? s.StartedAt).AddMinutes(30) <= now)
        {
            s.ClosedAt = (s.LastEventAt ?? s.StartedAt).AddMinutes(30);
            s.CloseReason = "idle_timeout";
            await db.SaveChangesAsync();
            s = null;
        }
        if (s is null)
        {
            s = new() { VisitorId = visitor, StartedAt = now };
            db.AnalyticsSessions.Add(s);
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        var options = AuthService.CookieOptions(c);
        options.IsEssential = false;
        options.MaxAge = TimeSpan.FromDays(90);
        c.Response.Cookies.Append(VisitorCookie, protector.Protect(visitor.ToString()), options);
        return new { sessionId = s.Id };
    }

    public async Task Withdraw(HttpContext c)
    {
        if (Visitor(c) is Guid visitor)
        {
            await db
                .AnalyticsSessions.Where(x => x.VisitorId == visitor && x.ClosedAt == null)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.ClosedAt, DateTimeOffset.UtcNow)
                        .SetProperty(x => x.CloseReason, "consent_withdrawn")
                );
        }
        c.Response.Cookies.Delete(VisitorCookie, AuthService.CookieOptions(c));
    }

    public async Task<object> Ingest(EventBatch batch, HttpContext c)
    {
        Rules.Require(batch.Events.Count is > 0 and <= 20, "Mỗi batch từ 1 đến 20 sự kiện.");
        Rules.Require(!c.User.IsInRole("Admin"), "Không ghi Analytics Admin.");
        var visitor = Visitor(c) ?? throw new ApiError(403, "Chưa có phiên Analytics được đồng ý.");
        var results = new List<object>();
        foreach (var input in batch.Events)
        {
            try
            {
                var status = await One(input, visitor, c);
                results.Add(new { eventId = input.EventId, status });
            }
            catch (ApiError e)
            {
                db.ChangeTracker.Clear();
                results.Add(
                    new
                    {
                        eventId = input.EventId,
                        status = e.Status == 409 ? "conflict" : "rejected",
                        error = e.Message,
                    }
                );
            }
            catch (DbUpdateException e)
                when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            {
                db.ChangeTracker.Clear();
                var stored = await db
                    .AnalyticsEvents.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == input.EventId);
                var incoming = SHA256.HashData(
                    JsonSerializer.SerializeToUtf8Bytes(
                        input with
                        {
                            ClientTimestamp = input.ClientTimestamp.ToUniversalTime(),
                        },
                        ContentService.Json
                    )
                );
                var owned =
                    stored is not null
                    && stored.SessionId == input.SessionId
                    && stored.PayloadHash.SequenceEqual(incoming)
                    && await db.AnalyticsSessions.AnyAsync(x =>
                        x.Id == stored.SessionId && x.VisitorId == visitor
                    );
                results.Add(
                    new { eventId = input.EventId, status = owned ? "duplicate" : "conflict" }
                );
            }
        }
        return new { results };
    }

    async Task<string> One(EventInput r, Guid visitor, HttpContext c)
    {
        Rules.Require(
            r.EventId != Guid.Empty && r.SessionId != Guid.Empty,
            "ID sự kiện/phiên không hợp lệ."
        );
        var hash = SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(
                r with
                {
                    ClientTimestamp = r.ClientTimestamp.ToUniversalTime(),
                },
                ContentService.Json
            )
        );
        await using var tx = await db.Database.BeginTransactionAsync();
        await Lock(visitor);
        var session =
            await db.AnalyticsSessions.SingleOrDefaultAsync(x =>
                x.Id == r.SessionId && x.VisitorId == visitor
            ) ?? throw new ApiError(403, "Phiên Analytics không hợp lệ.");
        var existing = await db
            .AnalyticsEvents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == r.EventId);
        if (existing is not null)
        {
            Rules.Require(
                existing.SessionId == session.Id && existing.PayloadHash.SequenceEqual(hash),
                "Event ID được dùng cho dữ liệu khác.",
                409
            );
            return "duplicate";
        }
        var now = DateTimeOffset.UtcNow;
        Rules.Require(
            session.ClosedAt == null
                && (session.LastEventAt ?? session.StartedAt).AddMinutes(30) > now,
            "Phiên Analytics đã hết hạn."
        );
        Rules.Require(throttle.Accept(session.Id), "Quá nhiều sự kiện, vui lòng chờ.", 429);
        Rules.Require(
            r.ClientTimestamp >= now.AddMinutes(-5) && r.ClientTimestamp <= now.AddMinutes(1),
            "Timestamp sự kiện đã quá hạn hoặc ở tương lai."
        );
        ValidateShape(r);
        if (r.PlayId is Guid play)
        {
            var semantic = await db
                .AnalyticsEvents.AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.SessionId == session.Id && x.PlayId == play && x.EventType == r.Type
                );
            if (semantic is not null)
            {
                Rules.Require(
                    semantic.PlaceId == r.PlaceId
                        && semantic.LanguageCode == r.LanguageCode
                        && semantic.Trigger == r.Trigger
                        && semantic.PlayedSeconds == r.PlayedSeconds,
                    "Play ID được dùng cho dữ liệu khác.",
                    409
                );
                return "duplicate";
            }
        }
        var ev = new AnalyticsEvent
        {
            Id = r.EventId,
            EventType = r.Type,
            SessionId = session.Id,
            PlaceId = r.PlaceId,
            LanguageCode = r.LanguageCode,
            PageKey = r.PageKey,
            PlayId = r.PlayId,
            Trigger = r.Trigger,
            ClientTimestamp = r.ClientTimestamp,
            ReceivedAt = now,
            PayloadHash = hash,
        };
        if (c.User.Identity?.IsAuthenticated == true && c.User.IsInRole("User"))
            ev.UserId = AuthService.Actor(c.User);
        if (r.Type == "narration_completed")
        {
            var start =
                await db
                    .AnalyticsEvents.AsNoTracking()
                    .SingleOrDefaultAsync(x =>
                        x.SessionId == session.Id
                        && x.PlayId == r.PlayId
                        && x.EventType == "narration_started"
                    )
                ?? throw new ApiError(422, "Chưa có lượt bắt đầu hợp lệ.");
            Rules.Require(
                start.PlaceId == r.PlaceId && start.LanguageCode == r.LanguageCode,
                "Lượt nghe không khớp địa điểm/ngôn ngữ."
            );
            ev.AudioAssetId = start.AudioAssetId;
            ev.DurationSeconds = start.DurationSeconds;
            ev.PlayStartedAt = start.PlayStartedAt;
            Rules.Require(
                r.PlayedSeconds >= start.DurationSeconds * .9m
                    && r.PlayedSeconds <= start.DurationSeconds + .1m
                    && (decimal)(now - start.ReceivedAt).TotalSeconds
                        >= start.DurationSeconds * .9m,
                "Chưa đạt điều kiện hoàn tất phát."
            );
            ev.PlayedSeconds = Math.Min(r.PlayedSeconds!.Value, start.DurationSeconds!.Value);
        }
        else if (r.PlaceId is Guid pid)
        {
            Rules.Require(
                await db.Places.AnyAsync(x => x.Id == pid && x.Status == "published"),
                "Địa điểm không công khai."
            );
            if (r.Type == "narration_started")
            {
                var narration =
                    await db
                        .Narrations.AsNoTracking()
                        .SingleOrDefaultAsync(x =>
                            x.PlaceId == pid && x.LanguageCode == r.LanguageCode
                        )
                    ?? throw new ApiError(422, "Thiếu bài thuyết minh.");
                var a =
                    await db
                        .AudioAssets.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == narration.AudioAssetId)
                    ?? throw new ApiError(422, "Không có âm thanh hợp lệ.");
                ev.AudioAssetId = a.Id;
                ev.DurationSeconds = a.DurationSeconds;
                ev.PlayStartedAt = now;
            }
        }
        db.AnalyticsEvents.Add(ev);
        session.LastEventAt = now;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return "accepted";
    }

    public static void ValidateShape(EventInput r)
    {
        Rules.Require(
            new[]
            {
                "page_view",
                "place_view",
                "location_permission_granted",
                "location_permission_denied",
                "geofence_entered",
                "narration_started",
                "narration_completed",
                "language_selected",
            }.Contains(r.Type),
            "Loại sự kiện không hợp lệ."
        );
        if (r.LanguageCode is not null)
            Rules.Locale(r.LanguageCode);
        var place =
            r.Type
            is "place_view"
                or "geofence_entered"
                or "narration_started"
                or "narration_completed";
        var play = r.Type is "narration_started" or "narration_completed";
        Rules.Require(
            place
                ? (r.PlaceId.HasValue && r.PlaceId != Guid.Empty && r.LanguageCode is not null)
                : r.PlaceId is null,
            "Trường địa điểm/ngôn ngữ không hợp lệ."
        );
        Rules.Require(
            play
                ? r.PlayId.HasValue && r.PlayId != Guid.Empty
                : r.PlayId is null
                    && r.DurationSeconds is null
                    && r.PlayedSeconds is null
                    && r.Trigger is null,
            "Trường lượt phát không hợp lệ."
        );
        Rules.Require(
            r.Type == "page_view"
                ? r.PageKey is "explore" or "place_detail" or "privacy"
                : r.PageKey is null,
            "Trang không hợp lệ."
        );
        if (r.Type == "language_selected")
            Rules.Require(r.LanguageCode is not null, "Cần mã ngôn ngữ.");
        if (r.Type == "narration_started")
            Rules.Require(
                r.Trigger is "manual" or "auto" && r.PlayedSeconds is null,
                "Nguồn phát không hợp lệ."
            );
        if (r.Type == "narration_completed")
            Rules.Require(r.PlayedSeconds.HasValue && r.Trigger is null, "Cần thời lượng đã phát.");
    }
}
