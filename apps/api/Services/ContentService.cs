using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Domain;

namespace SaigonGuide.Api.Services;

public class ContentService(GuideDb db, IWebHostEnvironment env, IConfiguration config)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static List<SourceRef> Sources(string json) =>
        JsonSerializer.Deserialize<List<SourceRef>>(json, Json) ?? [];

    public IQueryable<Place> Query() =>
        db.Places.Include(x => x.Translations).Include(x => x.Narrations).AsSplitQuery();

    public static string NormalizeSearch(string value) =>
        string.Concat(
                value
                    .Normalize(System.Text.NormalizationForm.FormD)
                    .Where(c =>
                        System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark
                    )
            )
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .ToLowerInvariant();

    public async Task<List<object>> PublicList(string lang, string? q)
    {
        Rules.Locale(lang);
        var places = await Query()
            .AsNoTracking()
            .Where(x => x.Status == "published")
            .OrderBy(x => x.Code)
            .ToListAsync();
        if (!string.IsNullOrWhiteSpace(q))
        {
            Rules.Require(q.Length <= 100, "Từ khóa tối đa 100 ký tự.");
            places = places
                .Where(x =>
                    x.Translations.Any(t =>
                        NormalizeSearch(t.Name)
                            .Contains(NormalizeSearch(q), StringComparison.Ordinal)
                    )
                )
                .ToList();
        }
        return places.Select(p => PublicDto(p, lang, false, null)).ToList();
    }

    public async Task<object> Detail(Guid id, string lang)
    {
        Rules.Locale(lang);
        var p =
            await Query()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id && x.Status == "published")
            ?? throw new ApiError(404, "Không tìm thấy địa điểm.");
        var n = p.Narrations.SingleOrDefault(x => x.LanguageCode == lang);
        var a = n?.AudioAssetId is Guid aid
            ? await db.AudioAssets.AsNoTracking().SingleAsync(x => x.Id == aid)
            : null;
        return PublicDto(p, lang, true, a);
    }

    static object PublicDto(Place p, string lang, bool detail, AudioAsset? audio)
    {
        var t = p.Translations.SingleOrDefault(x => x.LanguageCode == lang);
        var n = p.Narrations.SingleOrDefault(x => x.LanguageCode == lang);
        return new
        {
            p.Id,
            p.Code,
            Name = t?.Name ?? p.Code,
            Address = t?.Address,
            ShortDescription = t?.ShortDescription,
            p.Latitude,
            p.Longitude,
            p.TriggerRadiusM,
            p.ImageUrl,
            ImageAlt = t?.ImageAlt,
            p.ImageCredit,
            p.ImageLicense,
            p.IsDemo,
            p.VerificationStatus,
            LanguageCode = lang,
            Transcript = detail ? n?.Transcript : null,
            AudioAssetId = detail ? n?.AudioAssetId : null,
            DurationSeconds = audio?.DurationSeconds,
            Sources = detail ? Sources(n?.SourcesJson ?? p.SourcesJson) : [],
        };
    }

    public object EditorDto(Place p) =>
        new
        {
            p.Id,
            p.Code,
            p.Latitude,
            p.Longitude,
            p.TriggerRadiusM,
            p.Status,
            p.VerificationStatus,
            p.IsDemo,
            p.ImageUrl,
            p.ImageCredit,
            p.ImageLicense,
            Sources = Sources(p.SourcesJson),
            p.Version,
            p.UpdatedAt,
            Translations = p.Translations.Select(t => new
            {
                t.LanguageCode,
                t.Name,
                t.Address,
                t.ShortDescription,
                t.ImageAlt,
            }),
            Narrations = p.Narrations.Select(n => new
            {
                n.LanguageCode,
                n.Transcript,
                n.AudioAssetId,
                Sources = Sources(n.SourcesJson),
            }),
        };

    public async Task<Place> Locked(Guid id) =>
        await db
            .Places.FromSqlInterpolated($"SELECT * FROM places WHERE id={id} FOR UPDATE")
            .SingleOrDefaultAsync()
        ?? throw new ApiError(404, "Không tìm thấy địa điểm.");

    public static void Validate(PlaceInput r)
    {
        Rules.Require(
            Regex.IsMatch(r.Code, "^[a-z0-9]+(?:-[a-z0-9]+)*$") && r.Code.Length <= 80,
            "Mã địa điểm không hợp lệ."
        );
        Rules.Require(r.TriggerRadiusM is >= 25 and <= 500, "Bán kính phải từ 25 đến 500 m.");
        Rules.Require(
            r.Latitude.HasValue == r.Longitude.HasValue
                && (
                    !r.Latitude.HasValue
                    || (r.Latitude is >= -90 and <= 90 && r.Longitude is >= -180 and <= 180)
                ),
            "Tọa độ không hợp lệ."
        );
        Rules.Require(r.Status is "draft" or "published" or "hidden", "Trạng thái không hợp lệ.");
        Rules.Require(
            r.VerificationStatus is "verified" or "unverified",
            "Trạng thái xác minh không hợp lệ."
        );
        Rules.Sources(r.Sources);
        Rules.Require(
            r.Translations.Count <= 2
                && r.Narrations.Count <= 2
                && r.Translations.Select(x => x.LanguageCode).Distinct().Count()
                    == r.Translations.Count
                && r.Narrations.Select(x => x.LanguageCode).Distinct().Count()
                    == r.Narrations.Count,
            "Bản dịch không được trùng ngôn ngữ."
        );
        if (r.ImageUrl is not null)
        {
            var owned = Regex.IsMatch(r.ImageUrl, "^/images/[a-z0-9-]+\\.svg$");
            Rules.Require(
                owned
                    || (
                        Uri.TryCreate(r.ImageUrl, UriKind.Absolute, out var u)
                        && u.Scheme == "https"
                        && u.UserInfo == ""
                        && u.Host == "images.unsplash.com"
                    ),
                "Ảnh phải là minh họa có sẵn hoặc URL ảnh HTTPS từ nguồn cho phép."
            );
        }
        Rules.Require(
            (r.ImageUrl?.Length ?? 0) <= 2048
                && (r.ImageCredit?.Length ?? 0) <= 500
                && (r.ImageLicense?.Length ?? 0) <= 500,
            "Thông tin ảnh quá dài."
        );
        foreach (var t in r.Translations)
        {
            Rules.Locale(t.LanguageCode);
            Rules.Require(
                t.Name.Trim().Length is > 0 and <= 200
                    && (t.Address?.Length ?? 0) <= 500
                    && (t.ShortDescription?.Length ?? 0) <= 1000
                    && (t.ImageAlt?.Length ?? 0) <= 500,
                "Nội dung bản dịch vượt giới hạn."
            );
        }
        foreach (var n in r.Narrations)
        {
            Rules.Locale(n.LanguageCode);
            Rules.Require(
                (n.Transcript?.Length ?? 0) <= 20000,
                "Bài thuyết minh tối đa 20.000 ký tự."
            );
            Rules.Sources(n.Sources);
        }
    }

    public async Task<object> Save(Guid? id, PlaceInput r, Guid actor)
    {
        Validate(r);
        await using var tx = await db.Database.BeginTransactionAsync();
        Place p;
        if (id is Guid pid)
        {
            p = await Locked(pid);
            Rules.Require(
                p.Version == r.Version,
                "Nội dung đã được cập nhật. Vui lòng tải lại.",
                409
            );
            Rules.Require(p.Code == r.Code, "Không thể đổi mã địa điểm.");
            await db.Entry(p).Collection(x => x.Translations).LoadAsync();
            await db.Entry(p).Collection(x => x.Narrations).LoadAsync();
        }
        else
        {
            Rules.Require(r.Status == "draft", "Điểm mới phải bắt đầu ở trạng thái nháp.");
            Rules.Require(
                !await db.Places.AnyAsync(x => x.Code == r.Code),
                "Mã địa điểm đã tồn tại.",
                409
            );
            p = new Place { Code = r.Code, CreatedBy = actor };
            db.Places.Add(p);
        }
        p.Latitude = r.Latitude;
        p.Longitude = r.Longitude;
        p.TriggerRadiusM = r.TriggerRadiusM;
        p.Status = r.Status;
        p.VerificationStatus = r.VerificationStatus;
        p.IsDemo = r.IsDemo;
        p.ImageUrl = r.ImageUrl;
        p.ImageCredit = r.ImageCredit;
        p.ImageLicense = r.ImageLicense;
        p.SourcesJson = JsonSerializer.Serialize(r.Sources, Json);
        p.VerifiedAt = r.VerificationStatus == "verified" ? DateTimeOffset.UtcNow : null;
        p.VerifiedBy = r.VerificationStatus == "verified" ? actor : null;
        foreach (var t in r.Translations)
        {
            var target = p.Translations.SingleOrDefault(x => x.LanguageCode == t.LanguageCode);
            if (target is null)
            {
                target = new() { PlaceId = p.Id, LanguageCode = t.LanguageCode };
                p.Translations.Add(target);
            }
            target.Name = t.Name.Trim();
            target.Address = t.Address;
            target.ShortDescription = t.ShortDescription;
            target.ImageAlt = t.ImageAlt;
            target.UpdatedAt = DateTimeOffset.UtcNow;
        }
        foreach (
            var gone in p
                .Translations.Where(x => !r.Translations.Any(y => y.LanguageCode == x.LanguageCode))
                .ToArray()
        )
            db.PlaceTranslations.Remove(gone);
        foreach (var n in r.Narrations)
        {
            var target = p.Narrations.SingleOrDefault(x => x.LanguageCode == n.LanguageCode);
            if (target is null)
            {
                target = new() { PlaceId = p.Id, LanguageCode = n.LanguageCode };
                p.Narrations.Add(target);
            }
            var textChanged = target.Transcript?.Trim() != n.Transcript?.Trim();
            var effectiveAudio =
                textChanged
                && target.AudioAssetId is not null
                && target.AudioAssetId == n.AudioAssetId
                    ? (Guid?)null
                    : n.AudioAssetId;
            target.Transcript = n.Transcript;
            target.SourcesJson = JsonSerializer.Serialize(n.Sources, Json);
            target.AudioAssetId = effectiveAudio;
            target.UpdatedAt = DateTimeOffset.UtcNow;
        }
        foreach (
            var gone in p
                .Narrations.Where(x => !r.Narrations.Any(y => y.LanguageCode == x.LanguageCode))
                .ToArray()
        )
            db.Narrations.Remove(gone);
        var ids = p
            .Narrations.Where(x => x.AudioAssetId != null)
            .Select(x => x.AudioAssetId!.Value)
            .ToArray();
        var assets = await db.AudioAssets.Where(x => ids.Contains(x.Id)).ToListAsync();
        foreach (var n in p.Narrations.Where(x => x.AudioAssetId != null))
            Rules.Require(
                assets.Any(a =>
                    a.Id == n.AudioAssetId
                    && a.PlaceId == p.Id
                    && a.LanguageCode == n.LanguageCode
                    && a.Status == "ready"
                ),
                "Âm thanh không thuộc địa điểm/ngôn ngữ này."
            );
        if (p.Status == "published")
            Rules.Publish(p, assets, env.IsProduction());
        Touch(p, actor, id is null ? "create" : "update");
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return EditorDto(p);
    }

    public void Touch(Place p, Guid actor, string action)
    {
        p.Version++;
        p.UpdatedAt = DateTimeOffset.UtcNow;
        p.UpdatedBy = actor;
        db.AdminAuditLogs.Add(
            new()
            {
                ActorUserId = actor,
                Action = action,
                EntityId = p.Id,
                EntityVersion = p.Version,
                ChangeSummary = JsonSerializer.Serialize(new { toStatus = p.Status }, Json),
            }
        );
    }

    public async Task<object> Status(Guid id, StatusInput r, Guid actor)
    {
        Rules.Require(r.Status is "draft" or "published" or "hidden", "Trạng thái không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await Locked(id);
        Rules.Require(p.Version == r.Version, "Nội dung đã được cập nhật. Vui lòng tải lại.", 409);
        await db.Entry(p).Collection(x => x.Translations).LoadAsync();
        await db.Entry(p).Collection(x => x.Narrations).LoadAsync();
        p.Status = r.Status;
        if (p.Status == "published")
            Rules.Publish(
                p,
                await db.AudioAssets.Where(x => x.PlaceId == id).ToListAsync(),
                env.IsProduction()
            );
        Touch(
            p,
            actor,
            r.Status == "published" ? "publish"
                : r.Status == "hidden" ? "hide"
                : "update"
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return EditorDto(p);
    }

    public string MediaRoot { get; } =
        Path.GetFullPath(
            config["Media:Path"] ?? Path.Combine(env.ContentRootPath, "../../.local/media")
        );
}
