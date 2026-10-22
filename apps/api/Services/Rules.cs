using System.Net.Mail;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Domain;

namespace SaigonGuide.Api.Services;

public class ApiError(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public static class Rules
{
    public static string Locale(string? locale)
    {
        if (locale is not ("vi" or "en"))
            throw new ApiError(422, "Ngôn ngữ chỉ hỗ trợ vi hoặc en.");
        return locale;
    }

    public static void Require(bool ok, string message, int status = 422)
    {
        if (!ok)
            throw new ApiError(status, message);
    }

    public static void Email(string s)
    {
        Require(
            s.Length <= 254 && MailAddress.TryCreate(s, out var a) && a.Address == s,
            "Email không hợp lệ."
        );
    }

    public static void Password(string s) =>
        Require(s.Length is >= 12 and <= 128, "Mật khẩu cần từ 12 đến 128 ký tự.");

    public static void Sources(List<SourceRef> refs)
    {
        Require(refs.Count <= 10, "Tối đa 10 nguồn.");
        foreach (var s in refs)
        {
            Require(
                Uri.TryCreate(s.Url, UriKind.Absolute, out var u)
                    && u.Scheme == "https"
                    && u.UserInfo == ""
                    && s.Url.Length <= 2048,
                "Nguồn phải là URL HTTPS hợp lệ."
            );
            Require(
                s.Title.Length is > 0 and <= 200 && s.Scope.Length is > 0 and <= 500,
                "Thông tin nguồn không hợp lệ."
            );
            Require(
                DateOnly.TryParse(s.AccessedOn, out var date)
                    && date <= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)),
                "Ngày nguồn không hợp lệ."
            );
        }
    }

    public static void Publish(Place p, List<AudioAsset> assets, bool production)
    {
        Require(!production || !p.IsDemo, "Dữ liệu mẫu không được công khai ở production.");
        Require(
            p.VerificationStatus == "verified" && p.Latitude.HasValue && p.Longitude.HasValue,
            "Cần xác minh địa điểm và tọa độ."
        );
        Require(
            !string.IsNullOrWhiteSpace(p.ImageUrl)
                && !string.IsNullOrWhiteSpace(p.ImageCredit)
                && !string.IsNullOrWhiteSpace(p.ImageLicense)
                && p.SourcesJson != "[]",
            "Cần ảnh, quyền tài nguyên và nguồn."
        );
        foreach (var lang in new[] { "vi", "en" })
        {
            var t = p.Translations.SingleOrDefault(x => x.LanguageCode == lang);
            var n = p.Narrations.SingleOrDefault(x => x.LanguageCode == lang);
            Require(
                t is not null
                    && !string.IsNullOrWhiteSpace(t.Address)
                    && !string.IsNullOrWhiteSpace(t.ShortDescription)
                    && !string.IsNullOrWhiteSpace(t.ImageAlt),
                $"Thiếu thông tin địa điểm {lang}."
            );
            Require(
                n is not null
                    && !string.IsNullOrWhiteSpace(n.Transcript)
                    && n.SourcesJson != "[]"
                    && assets.Any(x =>
                        x.Id == n.AudioAssetId
                        && x.PlaceId == p.Id
                        && x.LanguageCode == lang
                        && x.Status == "ready"
                    ),
                $"Thiếu bài/âm thanh {lang} hợp lệ."
            );
        }
    }

    public static (DateTimeOffset Start, DateTimeOffset End, DateOnly From, DateOnly To) DateRange(
        string? from,
        string? to,
        DateTimeOffset now
    )
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime.AddHours(7));
        var f = today.AddDays(-6);
        var t = today;
        if (from is not null)
            Require(DateOnly.TryParse(from, out f), "Ngày bắt đầu không hợp lệ.");
        if (to is not null)
            Require(DateOnly.TryParse(to, out t), "Ngày kết thúc không hợp lệ.");
        Require(
            f <= t && t <= today && f >= today.AddDays(-89) && t.DayNumber - f.DayNumber < 90,
            "Khoảng thời gian phải nằm trong 90 ngày được lưu và không ở tương lai."
        );
        return (
            new DateTimeOffset(
                f.ToDateTime(TimeOnly.MinValue),
                TimeSpan.FromHours(7)
            ).ToUniversalTime(),
            new DateTimeOffset(
                t.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.FromHours(7)
            ).ToUniversalTime(),
            f,
            t
        );
    }
}
