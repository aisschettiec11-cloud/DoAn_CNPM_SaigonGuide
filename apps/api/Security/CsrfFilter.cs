using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using SaigonGuide.Api.Services;

namespace SaigonGuide.Api.Security;

public class CsrfFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext c)
    {
        if (
            HttpMethods.IsGet(c.HttpContext.Request.Method)
            || HttpMethods.IsHead(c.HttpContext.Request.Method)
            || HttpMethods.IsOptions(c.HttpContext.Request.Method)
        )
            return;
        try
        {
            await antiforgery.ValidateRequestAsync(c.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            throw new ApiError(403, "Phiên bảo vệ đã thay đổi. Vui lòng tải lại và thử lại.");
        }
    }
}
