using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Shortly.Api.Filters;

/// <summary>
/// Rejects requests containing invalid or expired Bearer tokens with a 401 Unauthorized response, 
/// preventing endpoints marked with [AllowAnonymous] from silently treating malformed tokens as anonymous.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RejectInvalidBearerTokenAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        // Ignore requests that do not provide a Bearer token
        var authorization = httpContext.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase)) 
            return;

        // Allow requests where the token successfully authenticated the user
        if (httpContext.User.Identity?.IsAuthenticated == true) 
            return;

        // If a Bearer token was provided but failed validation, explicitly return 401 with standard problem details
        httpContext.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
        context.Result = new ObjectResult(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Unauthorized",
            Detail = "The access token is invalid or has expired.",
            Status = StatusCodes.Status401Unauthorized
        })
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }
}