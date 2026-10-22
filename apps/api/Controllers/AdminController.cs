using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Api.Controllers;

[ApiController, Route("api/v1/admin"), Authorize(Roles = "Admin")]
public class AdminController(GuideDb db, DashboardService dashboard) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<object> Dashboard(
        string? from = null,
        string? to = null,
        string bucket = "day"
    ) => await dashboard.Get(from, to, bucket);

    [HttpGet("users")]
    public async Task<object> Users(
        string? q = null,
        string? status = null,
        int page = 1,
        int size = 20,
        string? from = null,
        string? to = null
    )
    {
        Rules.Require(page >= 1 && size is >= 1 and <= 100, "Phân trang không hợp lệ.");
        Rules.Require((q?.Length ?? 0) <= 100, "Từ khóa quá dài.");
        var query = db.Users.AsNoTracking().Where(x => x.Role == "User");
        if (!string.IsNullOrWhiteSpace(q))
        {
            var text = q.ToUpperInvariant();
            query = query.Where(x =>
                x.NormalizedEmail.Contains(text) || x.DisplayName.ToUpper().Contains(text)
            );
        }
        if (status is not null)
        {
            Rules.Require(status is "active" or "locked", "Trạng thái không hợp lệ.");
            query = query.Where(x => x.Status == status);
        }
        if (from is not null || to is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
            DateOnly? first = null,
                last = null;
            if (from is not null)
            {
                Rules.Require(
                    DateOnly.TryParse(from, out var d)
                        && d >= new DateOnly(1900, 1, 1)
                        && d <= today,
                    "Ngày đăng ký bắt đầu không hợp lệ."
                );
                first = d;
            }
            if (to is not null)
            {
                Rules.Require(
                    DateOnly.TryParse(to, out var d) && d >= new DateOnly(1900, 1, 1) && d <= today,
                    "Ngày đăng ký kết thúc không hợp lệ."
                );
                last = d;
            }
            Rules.Require(
                first is null || last is null || first <= last,
                "Khoảng ngày đăng ký bị đảo ngược."
            );
            if (first is DateOnly f)
            {
                var start = new DateTimeOffset(
                    f.ToDateTime(TimeOnly.MinValue),
                    TimeSpan.FromHours(7)
                ).ToUniversalTime();
                query = query.Where(x => x.CreatedAt >= start);
            }
            if (last is DateOnly l)
            {
                var end = new DateTimeOffset(
                    l.AddDays(1).ToDateTime(TimeOnly.MinValue),
                    TimeSpan.FromHours(7)
                ).ToUniversalTime();
                query = query.Where(x => x.CreatedAt < end);
            }
        }
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
        return new
        {
            items = items.Select(AuthService.Dto),
            total,
            page,
            size,
        };
    }

    [HttpPost("users/{id:guid}/status")]
    public async Task<object> Status(Guid id, StatusInput r)
    {
        Rules.Require(r.Status is "active" or "locked", "Trạng thái không hợp lệ.");
        Rules.Require(
            r.Status != "locked" || !string.IsNullOrWhiteSpace(r.Reason) && r.Reason.Length <= 500,
            "Cần lý do khóa tối đa 500 ký tự."
        );
        await using var tx = await db.Database.BeginTransactionAsync();
        var u =
            await db
                .Users.FromSqlInterpolated($"SELECT * FROM users WHERE id={id} FOR UPDATE")
                .SingleOrDefaultAsync()
            ?? throw new ApiError(404, "Không tìm thấy tài khoản.");
        Rules.Require(u.Role == "User", "Không thể quản lý Admin qua API tài khoản khách.", 403);
        Rules.Require(u.Version == r.Version, "Tài khoản đã được cập nhật. Vui lòng tải lại.", 409);
        u.Status = r.Status;
        u.LockedAt = r.Status == "locked" ? DateTimeOffset.UtcNow : null;
        u.LockReason = r.Status == "locked" ? r.Reason : null;
        u.Version++;
        u.UpdatedAt = DateTimeOffset.UtcNow;
        if (r.Status == "locked")
            await db
                .UserSessions.Where(x => x.UserId == id && x.RevokedAt == null)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow)
                        .SetProperty(x => x.RevokeReason, "account_locked")
                );
        db.AdminAuditLogs.Add(
            new()
            {
                ActorUserId = AuthService.Actor(User),
                EntityType = "User",
                EntityId = id,
                EntityVersion = u.Version,
                Action = r.Status == "locked" ? "account_lock" : "account_unlock",
                Reason = r.Reason,
            }
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return AuthService.Dto(u);
    }
}
