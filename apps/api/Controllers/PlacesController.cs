using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Api.Controllers;

[ApiController, Route("api/v1")]
public class PlacesController(ContentService content, GuideDb db, MediaService media)
    : ControllerBase
{
    [HttpGet("places")]
    public async Task<object> List(string language = "vi", string? q = null) =>
        await content.PublicList(language, q);

    [HttpGet("places/{id:guid}")]
    public async Task<object> Detail(Guid id, string language = "vi") =>
        await content.Detail(id, language);

    [HttpGet("media/{id:guid}")]
    public async Task<IActionResult> Audio(Guid id)
    {
        var a =
            await db.AudioAssets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new ApiError(404, "Không tìm thấy âm thanh.");
        if (!User.IsInRole("Admin"))
            Rules.Require(
                await db.Narrations.AnyAsync(n =>
                    n.AudioAssetId == id
                    && db.Places.Any(p => p.Id == n.PlaceId && p.Status == "published")
                ),
                "Không tìm thấy âm thanh.",
                404
            );
        var path = Path.Combine(content.MediaRoot, a.StorageKey);
        Rules.Require(System.IO.File.Exists(path), "Tệp âm thanh hiện không khả dụng.", 404);
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(path, "audio/mpeg", enableRangeProcessing: true);
    }

    [HttpGet("admin/places"), Authorize(Roles = "Admin")]
    public async Task<object> All() =>
        (
            await content.Query().AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToListAsync()
        ).Select(content.EditorDto);

    [HttpPost("admin/places"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(PlaceInput r) =>
        StatusCode(201, await content.Save(null, r, AuthService.Actor(User)));

    [HttpPut("admin/places/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<object> Save(Guid id, PlaceInput r) =>
        await content.Save(id, r, AuthService.Actor(User));

    [HttpPost("admin/places/{id:guid}/status"), Authorize(Roles = "Admin")]
    public async Task<object> Status(Guid id, StatusInput r) =>
        await content.Status(id, r, AuthService.Actor(User));

    [
        HttpPost("admin/places/{id:guid}/audio/{language}"),
        Authorize(Roles = "Admin"),
        RequestSizeLimit(21000000)
    ]
    public async Task<IActionResult> Upload(
        Guid id,
        string language,
        IFormFile file,
        [FromForm] long version,
        [FromForm] string credit,
        [FromForm] string license
    ) =>
        StatusCode(
            201,
            await media.Upload(
                id,
                language,
                file,
                version,
                credit,
                license,
                AuthService.Actor(User)
            )
        );
}
