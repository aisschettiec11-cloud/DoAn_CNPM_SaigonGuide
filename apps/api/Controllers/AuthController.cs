using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Api.Controllers;

[ApiController, Route("api/v1")]
public class AuthController(AuthService auth, IAntiforgery anti, GuideDb db) : ControllerBase
{
    [HttpGet("csrf")]
    public object Csrf()
    {
        Response.Headers.CacheControl = "no-store";
        return new { token = anti.GetAndStoreTokens(HttpContext).RequestToken };
    }

    [HttpGet("languages")]
    public async Task<object> Languages() =>
        await db.Languages.AsNoTracking().OrderBy(x => x.Code).ToListAsync();

    [HttpPost("auth/register")]
    public async Task<IActionResult> Register(RegisterInput r) =>
        StatusCode(201, await auth.Register(r));

    [HttpPost("auth/login")]
    public async Task<object> Login(LoginInput r) => await auth.Login(r, HttpContext);

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        await auth.Logout(HttpContext);
        return NoContent();
    }

    [HttpGet("auth/me"), Authorize]
    public async Task<object> Me() => AuthService.Dto(await auth.Current(User));

    [HttpPut("auth/profile"), Authorize]
    public async Task<object> Profile(ProfileInput r) => await auth.Profile(r, User);

    [HttpPost("auth/password"), Authorize]
    public async Task<IActionResult> Password(PasswordInput r)
    {
        await auth.ChangePassword(r, HttpContext);
        return NoContent();
    }
}
