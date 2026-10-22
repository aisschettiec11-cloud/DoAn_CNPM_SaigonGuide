using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Data;

namespace SaigonGuide.Api.Services;

public class RetentionWorker(IServiceScopeFactory scopes, ILogger<RetentionWorker> log)
    : BackgroundService
{
    public static DateTimeOffset Cutoff(DateTimeOffset now) =>
        new(
            DateOnly
                .FromDateTime(now.UtcDateTime.AddHours(7))
                .AddDays(-89)
                .ToDateTime(TimeOnly.MinValue),
            TimeSpan.FromHours(7)
        );

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GuideDb>();
                var now = DateTimeOffset.UtcNow;
                var cutoff = Cutoff(now).ToUniversalTime();
                var count = await db
                    .AnalyticsEvents.Where(x => x.ReceivedAt < cutoff)
                    .ExecuteDeleteAsync(stop);
                await db
                    .AnalyticsSessions.Where(x =>
                        x.ClosedAt == null && (x.LastEventAt ?? x.StartedAt).AddMinutes(30) < now
                    )
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(
                                    x => x.ClosedAt,
                                    x => (x.LastEventAt ?? x.StartedAt).AddMinutes(30)
                                )
                                .SetProperty(x => x.CloseReason, "idle_timeout"),
                        stop
                    );
                await db
                    .AnalyticsSessions.Where(x =>
                        x.ClosedAt != null
                        && (x.LastEventAt ?? x.StartedAt) < cutoff
                        && !db.AnalyticsEvents.Any(e => e.SessionId == x.Id)
                    )
                    .ExecuteDeleteAsync(stop);
                await db
                    .UserSessions.Where(x =>
                        (x.RevokedAt != null && x.RevokedAt < now.AddDays(-7))
                        || x.AbsoluteExpiresAt < now.AddDays(-7)
                        || x.LastSeenAt < now.AddDays(-7).AddMinutes(-30)
                    )
                    .ExecuteDeleteAsync(stop);
                await db
                    .AdminAuditLogs.Where(x => x.OccurredAt < now.AddDays(-180))
                    .ExecuteDeleteAsync(stop);
                var mediaRoot = scope
                    .ServiceProvider.GetRequiredService<ContentService>()
                    .MediaRoot;
                var unused = await db
                    .AudioAssets.AsNoTracking()
                    .Where(a =>
                        a.CreatedAt < now.AddHours(-24)
                        && !db.Narrations.Any(n => n.AudioAssetId == a.Id)
                        && !db.AnalyticsEvents.Any(e => e.AudioAssetId == a.Id)
                    )
                    .Select(a => new { a.Id, a.PlaceId })
                    .Take(100)
                    .ToListAsync(stop);
                foreach (var candidate in unused)
                {
                    await using var tx = await db.Database.BeginTransactionAsync(stop);
                    await db
                        .Places.FromSqlInterpolated(
                            $"SELECT * FROM places WHERE id={candidate.PlaceId} FOR UPDATE"
                        )
                        .AsNoTracking()
                        .SingleAsync(stop);
                    var asset = await db.AudioAssets.SingleOrDefaultAsync(
                        a => a.Id == candidate.Id,
                        stop
                    );
                    if (
                        asset is not null
                        && !await db.Narrations.AnyAsync(n => n.AudioAssetId == asset.Id, stop)
                        && !await db.AnalyticsEvents.AnyAsync(e => e.AudioAssetId == asset.Id, stop)
                    )
                    {
                        var key = asset.StorageKey;
                        db.AudioAssets.Remove(asset);
                        await db.SaveChangesAsync(stop);
                        await tx.CommitAsync(stop);
                        if (
                            System.Text.RegularExpressions.Regex.IsMatch(
                                key,
                                "^[a-f0-9]{32}\\.mp3$"
                            )
                        )
                        {
                            var file = Path.Combine(mediaRoot, key);
                            if (File.Exists(file))
                                File.Delete(file);
                        }
                    }
                }
                if (Directory.Exists(mediaRoot))
                {
                    var retained = await db
                        .AudioAssets.AsNoTracking()
                        .Select(a => a.StorageKey)
                        .ToHashSetAsync(stop);
                    foreach (var file in Directory.EnumerateFiles(mediaRoot, "*.mp3"))
                    {
                        var key = Path.GetFileName(file);
                        if (
                            !retained.Contains(key)
                            && File.GetLastWriteTimeUtc(file) < now.UtcDateTime.AddHours(-24)
                            && System.Text.RegularExpressions.Regex.IsMatch(
                                key,
                                "^[a-f0-9]{32}\\.mp3$"
                            )
                        )
                            File.Delete(file);
                    }
                }
                log.LogInformation(
                    "Retention completed: {Count} expired analytics events removed",
                    count
                );
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError("Retention failed ({Type}); retry in one hour", e.GetType().Name);
            }
            await Task.Delay(TimeSpan.FromHours(1), stop);
        }
    }
}
