using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Api.Controllers;

[ApiController, Route("api/v1/tiles")]
public class TilesController(IHttpClientFactory clients, ContentService content) : ControllerBase
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new();

    [HttpGet("{z:int}/{x:int}/{y:int}.png")]
    public async Task<IActionResult> Tile(int z, int x, int y, CancellationToken cancellationToken)
    {
        Rules.Require(
            z is >= 11 and <= 19 && x >= 0 && y >= 0 && x < (1 << z) && y < (1 << z),
            "Ô bản đồ không hợp lệ.",
            404
        );
        var n = Math.Pow(2, z);
        var longitude = (x + .5) / n * 360 - 180;
        var latitude = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (y + .5) / n))) * 180 / Math.PI;
        Rules.Require(
            longitude is >= 106.2 and <= 107.2 && latitude is >= 10.2 and <= 11.4,
            "Ô bản đồ ngoài khu vực hỗ trợ.",
            404
        );
        var key = $"{z}-{x}-{y}";
        var directory = Path.Combine(content.MediaRoot, "tiles");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, key + ".png");
        var gate = locks.GetOrAdd(key, _ => new(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (
                !System.IO.File.Exists(path)
                || System.IO.File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-7)
            )
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"https://a.tile.openstreetmap.org/{z}/{x}/{y}.png"
                );
                request.Headers.UserAgent.ParseAdd(
                    "SaigonGuide/1.0 (+https://github.com/aisschettiec11-cloud/DoAn_CNPM_SaigonGuide)"
                );
                request.Headers.Accept.ParseAdd("image/png");
                using var upstream = await clients
                    .CreateClient("tiles")
                    .SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken
                    );
                if (!upstream.IsSuccessStatusCode)
                    return StatusCode(502);
                if (
                    upstream.Content.Headers.ContentType?.MediaType != "image/png"
                    || upstream.Content.Headers.ContentLength > 1000000
                )
                    return StatusCode(502);
                var bytes = await upstream.Content.ReadAsByteArrayAsync(cancellationToken);
                if (
                    bytes.Length > 1000000
                    || bytes.Length < 8
                    || bytes[0] != 137
                    || bytes[1] != 80
                    || bytes[2] != 78
                    || bytes[3] != 71
                )
                    return StatusCode(502);
                var temp = path + ".tmp";
                await System.IO.File.WriteAllBytesAsync(temp, bytes, cancellationToken);
                System.IO.File.Move(temp, path, true);
            }
            Response.Headers.CacheControl = "public, max-age=604800";
            return PhysicalFile(path, "image/png");
        }
        catch (HttpRequestException)
        {
            return StatusCode(502);
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504);
        }
        finally
        {
            gate.Release();
        }
    }
}
