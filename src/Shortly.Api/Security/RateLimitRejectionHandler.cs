using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shortly.Api.HTMLTemplates;
using Shortly.Infrastructure.EdgeProtection.Models;

namespace Shortly.Api.Security;

/// <summary>
/// Handles HTTP 429 (Too Many Requests) rejections, returning HTML for browser forms or JSON for APIs.
/// </summary>
public static class RateLimitRejectionHandler
{
    public static async Task HandleAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var retryAfterSeconds = httpContext.Response.Headers.RetryAfter.ToString();
        var policyName = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        // Return an HTML password form for browser-based requests
        if (policyName == PasswordVerificationRateLimitOptions.PolicyName)
        {
            await RenderHtmlPasswordRejectionAsync(httpContext, retryAfterSeconds, cancellationToken);
            return;
        }

        // Return standard JSON Problem Details for API requests
        await RenderJsonProblemDetailsAsync(httpContext, policyName, cancellationToken);
    }

    /// <summary>
    /// Renders the password form template with a rate-limit error message.
    /// </summary>
    private static async Task RenderHtmlPasswordRejectionAsync(
        HttpContext context, 
        string retryAfter, 
        CancellationToken cancellationToken)
    {
        var token = context.Request.HasFormContentType
            ? (await context.Request.ReadFormAsync(cancellationToken))["token"].ToString()
            : string.Empty;

        var waitMessage = string.IsNullOrEmpty(retryAfter) 
            ? " Try again later." 
            : $" Try again in about {Math.Max(1, (int.Parse(retryAfter) + 59) / 60)} minute(s).";

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(
            UrlPasswordFormTemplate.Generate("Too many password attempts." + waitMessage, token), 
            cancellationToken);
    }

    /// <summary>
    /// Renders a standard JSON error response (RFC 6585) for API clients.
    /// </summary>
    private static async Task RenderJsonProblemDetailsAsync(
        HttpContext context, 
        string? policyName, 
        CancellationToken cancellationToken)
    {
        var detail = policyName == AnonymousShortUrlCreationRateLimitOptions.PolicyName
            ? "Anonymous short URL creation limit reached. Try again later or sign in."
            : "Too many requests. Try again later.";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Title = "Too many requests",
            Detail = detail,
            Status = StatusCodes.Status429TooManyRequests
        }, cancellationToken);
    }
}