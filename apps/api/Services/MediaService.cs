using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Domain;

namespace SaigonGuide.Api.Services;

public class MediaService(GuideDb db, ContentService content)
{
    public async Task<object> Upload(
        Guid id,
        string lang,
        IFormFile file,
        long version,
        string credit,
        string license,
        Guid actor
    )
    {
        Rules.Locale(lang);
        Rules.Require(file.Length is > 0 and <= 20000000, "Tệp âm thanh tối đa 20 MB.");
        Rules.Require(
            !string.IsNullOrWhiteSpace(credit)
                && credit.Length <= 500
                && !string.IsNullOrWhiteSpace(license)
                && license.Length <= 500,
            "Cần tác giả và quyền sử dụng âm thanh."
        );
        Directory.CreateDirectory(content.MediaRoot);
        var key = $"{Guid.NewGuid():N}.mp3";
        var path = Path.Combine(content.MediaRoot, key);
        var committed = false;
        try
        {
            await using (var stream = File.Create(path))
                await file.CopyToAsync(stream);
            var psi = new ProcessStartInfo("ffprobe")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (
                var arg in new[]
                {
                    "-v",
                    "error",
                    "-protocol_whitelist",
                    "file,crypto,data",
                    "-show_entries",
                    "format=duration,format_name",
                    "-of",
                    "default=noprint_wrappers=1",
                    path,
                }
            )
                psi.ArgumentList.Add(arg);
            using var process =
                Process.Start(psi) ?? throw new ApiError(422, "Không thể kiểm tra âm thanh.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(true);
                throw new ApiError(422, "Tệp âm thanh không xử lý được.");
            }
            var output = await outputTask;
            await errorTask;
            Rules.Require(
                process.ExitCode == 0
                    && output.Contains("format_name=mp3")
                    && decimal.TryParse(
                        output.Split('\n').FirstOrDefault(x => x.StartsWith("duration="))?[9..],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out _
                    ),
                "Tệp không phải MP3 hợp lệ."
            );
            var duration = decimal.Parse(
                output.Split('\n').First(x => x.StartsWith("duration="))[9..],
                CultureInfo.InvariantCulture
            );
            Rules.Require(
                duration > 0 && duration <= 600,
                "Thời lượng phải lớn hơn 0 và không quá 10 phút."
            );
            var decodeInfo = new ProcessStartInfo("ffmpeg")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            foreach (
                var arg in new[]
                {
                    "-nostdin",
                    "-v",
                    "error",
                    "-xerror",
                    "-protocol_whitelist",
                    "file,crypto,data",
                    "-i",
                    path,
                    "-f",
                    "null",
                    "-",
                }
            )
                decodeInfo.ArgumentList.Add(arg);
            using (
                var decode =
                    Process.Start(decodeInfo) ?? throw new ApiError(422, "Không thể kiểm tra tệp.")
            )
            {
                using var decodeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var errors = decode.StandardError.ReadToEndAsync(decodeTimeout.Token);
                var stdout = decode.StandardOutput.ReadToEndAsync(decodeTimeout.Token);
                try
                {
                    await decode.WaitForExitAsync(decodeTimeout.Token);
                }
                catch (OperationCanceledException)
                {
                    decode.Kill(true);
                    throw new ApiError(422, "Tệp âm thanh xử lý quá lâu.");
                }
                await errors;
                await stdout;
                Rules.Require(decode.ExitCode == 0, "Tệp MP3 bị lỗi giải mã.");
            }
            await using var tx = await db.Database.BeginTransactionAsync();
            var p = await content.Locked(id);
            Rules.Require(
                p.Version == version,
                "Nội dung đã được cập nhật. Vui lòng tải lại.",
                409
            );
            await db.Entry(p).Collection(x => x.Narrations).LoadAsync();
            var n = p.Narrations.SingleOrDefault(x => x.LanguageCode == lang);
            if (n is null)
            {
                n = new() { PlaceId = id, LanguageCode = lang };
                p.Narrations.Add(n);
            }
            await using var read = File.OpenRead(path);
            var asset = new AudioAsset
            {
                PlaceId = id,
                LanguageCode = lang,
                StorageKey = key,
                ByteSize = file.Length,
                DurationSeconds = decimal.Round(duration, 3),
                Sha256 = await SHA256.HashDataAsync(read),
                Credit = credit,
                License = license,
                UploadedBy = actor,
            };
            db.AudioAssets.Add(asset);
            n.AudioAssetId = asset.Id;
            n.UpdatedAt = DateTimeOffset.UtcNow;
            content.Touch(p, actor, "upload");
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            committed = true;
            return new
            {
                asset.Id,
                asset.DurationSeconds,
                Version = p.Version,
            };
        }
        finally
        {
            if (!committed && File.Exists(path))
                File.Delete(path);
        }
    }
}
