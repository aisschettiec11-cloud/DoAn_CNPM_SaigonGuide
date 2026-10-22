using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Domain;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Tests;

public class RulesTests
{
    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData("VI")]
    public void LocaleRejectsUnsupported(string s) =>
        Assert.Throws<ApiError>(() => Rules.Locale(s));

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public void PasswordMinimum(string s) => Assert.Throws<ApiError>(() => Rules.Password(s));

    [Fact]
    public void TimezoneInclusiveRange()
    {
        var r = Rules.DateRange(
            "2026-10-21",
            "2026-10-22",
            DateTimeOffset.Parse("2026-10-22T12:00:00Z")
        );
        Assert.Equal(DateTimeOffset.Parse("2026-10-20T17:00:00Z"), r.Start);
        Assert.Equal(DateTimeOffset.Parse("2026-10-22T17:00:00Z"), r.End);
    }

    [Fact]
    public void RejectsFutureAndReversedRanges()
    {
        var now = DateTimeOffset.Parse("2026-10-22T12:00:00Z");
        Assert.Throws<ApiError>(() => Rules.DateRange("2026-10-23", "2026-10-22", now));
        Assert.Throws<ApiError>(() => Rules.DateRange("2026-10-22", "2026-10-23", now));
    }

    [Fact]
    public void PublishRejectsMissingTranslationAudioAndDemoProduction()
    {
        var p = new Place
        {
            VerificationStatus = "verified",
            Latitude = 10,
            Longitude = 106,
            ImageUrl = "/images/a.svg",
            ImageCredit = "a",
            ImageLicense = "CC0",
            SourcesJson = "[{}]",
        };
        Assert.Throws<ApiError>(() => Rules.Publish(p, [], false));
        p.IsDemo = true;
        Assert.Throws<ApiError>(() => Rules.Publish(p, [], true));
    }

    [Fact]
    public void SourcesRequireHttpsAndValidDate() =>
        Assert.Throws<ApiError>(() =>
            Rules.Sources([new("http://example.com", "a", "2026-10-22", "a")])
        );

    [Fact]
    public void InvalidEventShapeCannotInjectUnrelatedPlace() =>
        Assert.Throws<ApiError>(() =>
            AnalyticsService.ValidateShape(
                new(
                    Guid.NewGuid(),
                    "page_view",
                    DateTimeOffset.UtcNow,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    PageKey: "explore"
                )
            )
        );

    [Fact]
    public void CompletionRequiresPlaybackDuration() =>
        Assert.Throws<ApiError>(() =>
            AnalyticsService.ValidateShape(
                new(
                    Guid.NewGuid(),
                    "narration_completed",
                    DateTimeOffset.UtcNow,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "vi",
                    PlayId: Guid.NewGuid()
                )
            )
        );

    [Fact]
    public void CalendarBucketsUseVietnameseTime()
    {
        Assert.Equal(
            "2026-10-23",
            DashboardService.Bucket(DateTimeOffset.Parse("2026-10-22T17:00:00Z"), "day")
        );
        Assert.Equal(
            "2026-10-19",
            DashboardService.Bucket(DateTimeOffset.Parse("2026-10-22T17:00:00Z"), "week")
        );
    }

    [Fact]
    public void RetentionKeepsNinetyVietnameseCalendarDays()
    {
        var now = DateTimeOffset.Parse("2026-10-22T18:00:00Z");
        var cutoff = RetentionWorker.Cutoff(now);
        Assert.Equal(
            89,
            (
                DateOnly.FromDateTime(now.UtcDateTime.AddHours(7)).DayNumber
                - DateOnly.FromDateTime(cutoff.DateTime).DayNumber
            )
        );
        Assert.Equal(TimeSpan.FromHours(7), cutoff.Offset);
    }
}
