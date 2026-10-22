using Microsoft.AspNetCore.Mvc;
using SaigonGuide.Api.Contracts;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Api.Controllers;

[ApiController, Route("api/v1/analytics")]
public class AnalyticsController(AnalyticsService analytics) : ControllerBase
{
    [HttpPost("session")]
    public async Task<object> Session(ConsentInput r) =>
        await analytics.Session(r.Consent, HttpContext);

    [HttpDelete("session")]
    public async Task<IActionResult> Withdraw()
    {
        await analytics.Withdraw(HttpContext);
        return NoContent();
    }

    [HttpPost("events"), RequestSizeLimit(32768)]
    public async Task<object> Events(EventBatch r) => await analytics.Ingest(r, HttpContext);
}
