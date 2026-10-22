using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Data;

namespace SaigonGuide.Api.Services;

public class TrafficRow
{
    public DateTime Date { get; set; }
    public int PageViews { get; set; }
    public int Sessions { get; set; }
}

public class DashboardService(GuideDb db)
{
    public static string Bucket(DateTimeOffset time, string bucket)
    {
        var day = DateOnly.FromDateTime(time.UtcDateTime.AddHours(7));
        return bucket switch
        {
            "week" => day.AddDays(-(((int)day.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd"),
            "month" => new DateOnly(day.Year, day.Month, 1).ToString("yyyy-MM-dd"),
            _ => day.ToString("yyyy-MM-dd"),
        };
    }

    public async Task<object> Get(string? from, string? to, string bucket)
    {
        Rules.Require(bucket is "day" or "week" or "month", "Nhóm thời gian không hợp lệ.");
        var r = Rules.DateRange(from, to, DateTimeOffset.UtcNow);
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.RepeatableRead
        );
        var query = db
            .AnalyticsEvents.AsNoTracking()
            .Where(x => x.ReceivedAt >= r.Start && x.ReceivedAt < r.End);
        var counts = await query
            .GroupBy(x => x.EventType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
        int Count(string type) => counts.GetValueOrDefault(type);
        var sessions = await query.Select(x => x.SessionId).Distinct().CountAsync();
        var visitors = await db
            .AnalyticsSessions.Where(s => query.Any(e => e.SessionId == s.Id))
            .Select(x => x.VisitorId)
            .Distinct()
            .CountAsync();
        var activeUsers = await query
            .Where(x => x.UserId != null)
            .Select(x => x.UserId)
            .Distinct()
            .CountAsync();
        var registered = await db.Users.CountAsync(x => x.Role == "User" && x.CreatedAt < r.End);
        var cohort = await query
            .Where(x => x.EventType == "narration_started")
            .CountAsync(s =>
                db.AnalyticsEvents.Any(c =>
                    c.EventType == "narration_completed"
                    && c.SessionId == s.SessionId
                    && c.PlayId == s.PlayId
                    && c.ReceivedAt >= s.ReceivedAt
                    && c.ReceivedAt < r.End
                )
            );
        var perPlace = await query
            .Where(x => x.PlaceId != null)
            .GroupBy(x => x.PlaceId)
            .Select(g => new
            {
                Id = g.Key,
                Views = g.Count(x => x.EventType == "place_view"),
                Sessions = g.Where(x => x.EventType == "place_view")
                    .Select(x => x.SessionId)
                    .Distinct()
                    .Count(),
                Entries = g.Count(x => x.EventType == "geofence_entered"),
                Starts = g.Count(x => x.EventType == "narration_started"),
                Completions = g.Count(x => x.EventType == "narration_completed"),
            })
            .ToListAsync();
        var conversions = await query
            .Where(v =>
                v.EventType == "place_view"
                && query.Any(s =>
                    s.EventType == "narration_started"
                    && s.PlaceId == v.PlaceId
                    && s.SessionId == v.SessionId
                    && s.ReceivedAt >= v.ReceivedAt
                )
            )
            .GroupBy(x => x.PlaceId)
            .Select(g => new { Id = g.Key, Count = g.Select(x => x.SessionId).Distinct().Count() })
            .ToListAsync();
        var places = await db.Places.Include(x => x.Translations).AsNoTracking().ToListAsync();
        var rows = places
            .Select(p =>
            {
                var stats = perPlace.SingleOrDefault(x => x.Id == p.Id);
                var conversion = conversions.SingleOrDefault(x => x.Id == p.Id)?.Count ?? 0;
                return new
                {
                    id = p.Id,
                    name = p.Translations.FirstOrDefault(x => x.LanguageCode == "vi")?.Name
                        ?? p.Code,
                    status = p.Status,
                    views = stats?.Views ?? 0,
                    sessions = stats?.Sessions ?? 0,
                    entries = stats?.Entries ?? 0,
                    starts = stats?.Starts ?? 0,
                    completions = stats?.Completions ?? 0,
                    startRate = stats is null || stats.Sessions == 0
                        ? (double?)null
                        : 100d * conversion / stats.Sessions,
                };
            })
            .OrderByDescending(x => x.views)
            .ThenBy(x => x.id)
            .ToList();
        var trafficDb = await db
            .Database.SqlQuery<TrafficRow>(
                $"SELECT date_trunc({bucket}, received_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AS \"Date\", (count(*) FILTER (WHERE event_type='page_view'))::int AS \"PageViews\", count(DISTINCT session_id)::int AS \"Sessions\" FROM analytics_events WHERE received_at >= {r.Start} AND received_at < {r.End} GROUP BY 1 ORDER BY 1"
            )
            .ToListAsync();
        var dates = Enumerable
            .Range(0, r.To.DayNumber - r.From.DayNumber + 1)
            .Select(i => r.From.AddDays(i))
            .Select(d =>
                Bucket(
                    new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)),
                    bucket
                )
            )
            .Distinct();
        var traffic = dates
            .Select(d =>
            {
                var v = trafficDb.FirstOrDefault(x => x.Date.ToString("yyyy-MM-dd") == d);
                return new
                {
                    date = d,
                    pageViews = v?.PageViews ?? 0,
                    sessions = v?.Sessions ?? 0,
                };
            })
            .ToList();
        var langCounts = await query
            .Where(x =>
                x.LanguageCode != null
                && (x.EventType == "language_selected" || x.EventType == "narration_started")
            )
            .GroupBy(x => new { x.LanguageCode, x.EventType })
            .Select(g => new
            {
                g.Key.LanguageCode,
                g.Key.EventType,
                Count = g.Count(),
            })
            .ToListAsync();
        var languages = new[] { "vi", "en" }
            .Select(lang => new
            {
                code = lang,
                selections = langCounts
                    .SingleOrDefault(x =>
                        x.LanguageCode == lang && x.EventType == "language_selected"
                    )
                    ?.Count
                    ?? 0,
                starts = langCounts
                    .SingleOrDefault(x =>
                        x.LanguageCode == lang && x.EventType == "narration_started"
                    )
                    ?.Count
                    ?? 0,
            })
            .ToList();
        await tx.CommitAsync();
        var starts = Count("narration_started");
        return new
        {
            range = new
            {
                from = r.From.ToString("yyyy-MM-dd"),
                to = r.To.ToString("yyyy-MM-dd"),
                timeZone = "Asia/Ho_Chi_Minh",
                bucket,
            },
            kpis = new
            {
                pageViews = Count("page_view"),
                sessions,
                visitors,
                placeViews = Count("place_view"),
                narrationStarts = starts,
                narrationCompletions = Count("narration_completed"),
                registeredUsers = registered,
                activeUsers,
                completionRate = starts == 0 ? (double?)null : 100d * cohort / starts,
            },
            traffic,
            places = rows,
            languages,
        };
    }
}
