using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Domain;
using SaigonGuide.Api.Security;

namespace SaigonGuide.Api.Services;

public class LoginGate
{
    readonly Dictionary<string, (int Count, DateTimeOffset Until)> entries = [];
    readonly object sync = new();

    public void Check(string key)
    {
        lock (sync)
        {
            if (
                entries.TryGetValue(key, out var v)
                && v.Until > DateTimeOffset.UtcNow
                && v.Count >= 5
            )
                throw new ApiError(
                    429,
                    "Quá nhiều lần đăng nhập sai. Vui lòng thử lại sau 15 phút."
                );
        }
    }

    public void Fail(string key)
    {
        lock (sync)
        {
            var n = DateTimeOffset.UtcNow;
            if (entries.Count > 10000)
            {
                foreach (
                    var k in entries.Where(x => x.Value.Until < n).Select(x => x.Key).ToArray()
                )
                    entries.Remove(k);
            }
            var v = entries.GetValueOrDefault(key);
            entries[key] = v.Until > n ? (v.Count + 1, v.Until) : (1, n.AddMinutes(15));
        }
    }
}

public class AuthService(GuideDb db, IPasswordHasher<User> hasher, LoginGate gate)
{
    static readonly Lazy<string> DummyHash = new(() =>
        new PasswordHasher<User>().HashPassword(new User(), "timing-only-value-not-an-account")
    );

    public static Guid Actor(ClaimsPrincipal principal) =>
        Guid.Parse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new ApiError(401, "Vui lòng đăng nhập.")
        );

    public static object Dto(User u) =>
        new
        {
            u.Id,
            u.Email,
            u.DisplayName,
            u.Role,
            u.Status,
            u.PreferredLocale,
            u.Version,
            u.CreatedAt,
        };

    public async Task<User> Current(ClaimsPrincipal p) =>
        await db.Users.SingleOrDefaultAsync(x => x.Id == Actor(p))
        ?? throw new ApiError(401, "Phiên không hợp lệ.");

    public async Task<object> Register(RegisterInput r)
    {
        Rules.Email(r.Email.Trim());
        Rules.Password(r.Password);
        Rules.Require(r.DisplayName.Trim().Length is > 0 and <= 100, "Tên cần từ 1 đến 100 ký tự.");
        var normalized = r.Email.Trim().ToUpperInvariant();
        Rules.Require(
            !await db.Users.AnyAsync(x => x.NormalizedEmail == normalized),
            "Không thể tạo tài khoản với thông tin đã cung cấp.",
            409
        );
        var u = new User
        {
            Email = r.Email.Trim(),
            NormalizedEmail = normalized,
            DisplayName = r.DisplayName.Trim(),
        };
        u.PasswordHash = hasher.HashPassword(u, r.Password);
        db.Users.Add(u);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
            when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            throw new ApiError(409, "Không thể tạo tài khoản với thông tin đã cung cấp.");
        }
        return Dto(u);
    }

    public async Task<object> Login(LoginInput r, HttpContext c)
    {
        Rules.Require(
            r.Email.Length <= 254 && r.Password.Length <= 128,
            "Email hoặc mật khẩu không hợp lệ.",
            401
        );
        var key = Convert.ToHexString(SessionHandler.Hash(r.Email.Trim().ToUpperInvariant()));
        var ip = "ip:" + c.Connection.RemoteIpAddress;
        gate.Check(key);
        gate.Check(ip);
        var u = await db.Users.SingleOrDefaultAsync(x =>
            x.NormalizedEmail == r.Email.Trim().ToUpperInvariant()
        );
        var passwordOk =
            hasher.VerifyHashedPassword(
                u ?? new User(),
                u?.PasswordHash ?? DummyHash.Value,
                r.Password
            ) != PasswordVerificationResult.Failed;
        if (u is null || !passwordOk || u.Status != "active")
        {
            gate.Fail(key);
            gate.Fail(ip);
            throw new ApiError(401, "Email hoặc mật khẩu không hợp lệ.");
        }
        await using var tx = await db.Database.BeginTransactionAsync();
        var locked = await db
            .Users.FromSqlInterpolated($"SELECT * FROM users WHERE id={u.Id} FOR UPDATE")
            .AsNoTracking()
            .SingleAsync();
        Rules.Require(
            locked.Status == "active"
                && locked.Version == u.Version
                && locked.PasswordHash == u.PasswordHash,
            "Email hoặc mật khẩu không hợp lệ.",
            401
        );
        await Logout(c);
        var now = DateTimeOffset.UtcNow;
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        db.UserSessions.Add(
            new UserSession
            {
                UserId = u.Id,
                HandleHash = SessionHandler.Hash(token),
                CreatedAt = now,
                LastSeenAt = now,
                AbsoluteExpiresAt = now.AddHours(8),
            }
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        c.Response.Cookies.Append(SessionHandler.Cookie, token, CookieOptions(c));
        return Dto(u);
    }

    public static CookieOptions CookieOptions(HttpContext c) =>
        new()
        {
            HttpOnly = true,
            Secure =
                c.Request.IsHttps
                || c.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
        };

    public async Task Logout(HttpContext c)
    {
        if (c.Request.Cookies.TryGetValue(SessionHandler.Cookie, out var token))
        {
            var h = SessionHandler.Hash(token);
            await db
                .UserSessions.Where(x => x.HandleHash == h && x.RevokedAt == null)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow)
                        .SetProperty(x => x.RevokeReason, "logout")
                );
        }
        c.Response.Cookies.Delete(SessionHandler.Cookie, CookieOptions(c));
    }

    public async Task<object> Profile(ProfileInput r, ClaimsPrincipal p)
    {
        Rules.Require(r.DisplayName.Trim().Length is > 0 and <= 100, "Tên không hợp lệ.");
        Rules.Locale(r.PreferredLocale);
        var u = await Current(p);
        u.DisplayName = r.DisplayName.Trim();
        u.PreferredLocale = r.PreferredLocale;
        u.Version++;
        u.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Dto(u);
    }

    public async Task ChangePassword(PasswordInput r, HttpContext c)
    {
        Rules.Password(r.NewPassword);
        await using var tx = await db.Database.BeginTransactionAsync();
        var id = Actor(c.User);
        var u = await db
            .Users.FromSqlInterpolated($"SELECT * FROM users WHERE id={id} FOR UPDATE")
            .SingleAsync();
        Rules.Require(
            hasher.VerifyHashedPassword(u, u.PasswordHash, r.CurrentPassword)
                != PasswordVerificationResult.Failed,
            "Mật khẩu hiện tại không đúng."
        );
        u.PasswordHash = hasher.HashPassword(u, r.NewPassword);
        u.Version++;
        u.UpdatedAt = DateTimeOffset.UtcNow;
        await db
            .UserSessions.Where(x => x.UserId == id && x.RevokedAt == null)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow)
                    .SetProperty(x => x.RevokeReason, "password_changed")
            );
        db.AdminAuditLogs.Add(
            new()
            {
                ActorUserId = id,
                Action = "password_change",
                EntityType = "User",
                EntityId = id,
                EntityVersion = u.Version,
            }
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        c.Response.Cookies.Delete(SessionHandler.Cookie, CookieOptions(c));
    }
}
