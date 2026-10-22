using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaigonGuide.Api.Data;

namespace SaigonGuide.Api.Security;

public class SessionHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    GuideDb db
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Cookie = "sg_session";

    public static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(Cookie, out var token))
            return AuthenticateResult.NoResult();
        if (token.Length > 100)
            return AuthenticateResult.Fail("Invalid session");
        var hash = Hash(token);
        var now = DateTimeOffset.UtcNow;
        var session = await db
            .UserSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.HandleHash == hash);
        if (
            session is null
            || session.RevokedAt is not null
            || session.AbsoluteExpiresAt <= now
            || session.LastSeenAt.AddMinutes(30) <= now
        )
            return AuthenticateResult.Fail("Expired session");
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == session.UserId);
        if (user is null || user.Status != "active")
            return AuthenticateResult.Fail("Account unavailable");
        await db
            .UserSessions.Where(x => x.Id == session.Id && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, now));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
        };
        return AuthenticateResult.Success(
            new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)),
                Scheme.Name
            )
        );
    }
}
