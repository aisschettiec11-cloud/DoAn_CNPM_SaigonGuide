using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Domain;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Api.Data;

public class SeedTranslation
{
    public string LanguageCode { get; set; } = "vi";
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Transcript { get; set; } = "";
    public string AudioFile { get; set; } = "";
    public decimal DurationSeconds { get; set; }
}

public class SeedPlace
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public List<SaigonGuide.Api.Contracts.SourceRef> Sources { get; set; } = [];
    public List<SeedTranslation> Translations { get; set; } = [];
}

public static class Seeder
{
    public static async Task Seed(
        GuideDb db,
        ContentService content,
        IConfiguration config,
        IWebHostEnvironment env
    )
    {
        var path = Path.GetFullPath(
            config["Content:Path"] ?? Path.Combine(env.ContentRootPath, "../../content")
        );
        var records =
            JsonSerializer.Deserialize<List<SeedPlace>>(
                await File.ReadAllTextAsync(Path.Combine(path, "places.json")),
                ContentService.Json
            ) ?? [];
        Directory.CreateDirectory(content.MediaRoot);
        foreach (var r in records)
        {
            if (await db.Places.AnyAsync(x => x.Id == r.Id))
                continue;
            var p = new Place
            {
                Id = r.Id,
                Code = r.Code,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Status = "published",
                VerificationStatus = "verified",
                VerifiedAt = DateTimeOffset.UtcNow,
                ImageUrl = $"/images/{r.Code}.svg",
                ImageCredit = "Saigon Guide — minh họa nguyên bản",
                ImageLicense = "CC0-1.0",
                SourcesJson = JsonSerializer.Serialize(r.Sources, ContentService.Json),
            };
            db.Places.Add(p);
            var assets = new List<AudioAsset>();
            foreach (var t in r.Translations)
            {
                var original = Path.Combine(path, "audio", t.AudioFile);
                var key = $"{Guid.NewGuid():N}.mp3";
                using (var sourceFile = File.OpenRead(original))
                using (var destination = File.Create(Path.Combine(content.MediaRoot, key)))
                    await sourceFile.CopyToAsync(destination);
                await using var stream = File.OpenRead(original);
                var a = new AudioAsset
                {
                    PlaceId = p.Id,
                    LanguageCode = t.LanguageCode,
                    StorageKey = key,
                    ByteSize = stream.Length,
                    DurationSeconds = t.DurationSeconds,
                    Sha256 = await SHA256.HashDataAsync(stream),
                    Credit =
                        "Saigon Guide; giọng tổng hợp eSpeak NG; nội dung biên soạn từ nguồn ghi kèm",
                    License = "CC BY 4.0 — Saigon Guide",
                };
                db.AudioAssets.Add(a);
                assets.Add(a);
                p.Translations.Add(
                    new()
                    {
                        PlaceId = p.Id,
                        LanguageCode = t.LanguageCode,
                        Name = t.Name,
                        Address = t.Address,
                        ShortDescription = t.ShortDescription,
                        ImageAlt = $"Minh họa kiến trúc: {t.Name}",
                    }
                );
                p.Narrations.Add(
                    new()
                    {
                        PlaceId = p.Id,
                        LanguageCode = t.LanguageCode,
                        Transcript = t.Transcript,
                        AudioAssetId = a.Id,
                        SourcesJson = p.SourcesJson,
                    }
                );
            }
            Rules.Publish(p, assets, env.IsProduction());
            await db.SaveChangesAsync();
        }
    }

    public static async Task Admin(GuideDb db, IConfiguration config, IPasswordHasher<User> hasher)
    {
        var email =
            config["Bootstrap:AdminEmail"]
            ?? throw new InvalidOperationException("Set Bootstrap__AdminEmail securely.");
        var password =
            config["Bootstrap:AdminPassword"]
            ?? throw new InvalidOperationException("Set Bootstrap__AdminPassword securely.");
        Rules.Email(email);
        Rules.Password(password);
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == email.ToUpperInvariant()))
            throw new InvalidOperationException(
                "Account exists; bootstrap never overwrites credentials."
            );
        var u = new User
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = "Quản trị viên",
            Role = "Admin",
        };
        u.PasswordHash = hasher.HashPassword(u, password);
        db.Users.Add(u);
        await db.SaveChangesAsync();
    }
}
