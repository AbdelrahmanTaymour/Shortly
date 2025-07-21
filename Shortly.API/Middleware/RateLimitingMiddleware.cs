using System.Net;
using Microsoft.Extensions.Caching.Memory;

namespace Shortly.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var rateLimitAttribute = endpoint?.Metadata?.GetMetadata<RateLimitAttribute>();

        if (rateLimitAttribute != null)
        {
            var clientId = GetClientIdentifier(context);
            var key = $"rate_limit_{clientId}_{endpoint?.DisplayName}";

            var requests = _cache.Get<List<DateTime>>(key) ?? new List<DateTime>();
            var now = DateTime.UtcNow;

            // Remove old requests outside the time window
            requests.RemoveAll(r => r < now.AddSeconds(-rateLimitAttribute.WindowSeconds));

            if (requests.Count >= rateLimitAttribute.MaxRequests)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            requests.Add(now);
            _cache.Set(key, requests, TimeSpan.FromSeconds(rateLimitAttribute.WindowSeconds));
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use API key if available, otherwise use IP address
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
            return apiKey;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitAttribute : Attribute
{
    public int MaxRequests { get; }
    public int WindowSeconds { get; }

    public RateLimitAttribute(int maxRequests = 100, int windowSeconds = 3600)
    {
        MaxRequests = maxRequests;
        WindowSeconds = windowSeconds;
    }
}