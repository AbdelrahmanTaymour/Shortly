using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Shortly.API.Middleware;

/// <summary>
///     Intelligent response caching middleware for improved performance.
///     Caches responses based on request characteristics and implements smart cache invalidation.
/// </summary>
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCachingMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    // Cache configuration
    private const int DefaultCacheMinutes = 5;
    private const int MaxCacheSize = 1000; // Maximum number of cached responses

    public ResponseCachingMiddleware(RequestDelegate next, ILogger<ResponseCachingMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip caching for non-GET requests and certain endpoints
        if (!ShouldCacheRequest(context))
        {
            await _next(context);
            return;
        }

        var cacheKey = GenerateCacheKey(context);
        
        // Try to get cached response
        if (_cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse))
        {
            await ServeCachedResponseAsync(context, cachedResponse);
            return;
        }

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            // Only cache successful responses
            if (context.Response.StatusCode == 200)
            {
                await CacheResponseAsync(context, memoryStream, cacheKey);
            }

            // Copy response back to original stream
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldCacheRequest(HttpContext context)
    {
        // Only cache GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
            return false;

        // Skip caching for authenticated requests (unless explicitly allowed)
        if (context.User.Identity?.IsAuthenticated == true)
            return false;

        // Skip caching for certain endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (path == null) return false;

        var nonCacheablePaths = new[]
        {
            "/api/auth",
            "/api/admin",
            "/swagger",
            "/health"
        };

        return !nonCacheablePaths.Any(p => path.StartsWith(p));
    }

    private string GenerateCacheKey(HttpContext context)
    {
        var keyBuilder = new StringBuilder();
        
        // Include path and query string
        keyBuilder.Append(context.Request.Path.Value);
        if (!string.IsNullOrEmpty(context.Request.QueryString.Value))
        {
            keyBuilder.Append(context.Request.QueryString.Value);
        }

        // Include user agent for different cache entries per client type
        var userAgent = context.Request.Headers.UserAgent.ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            keyBuilder.Append("|UA:");
            keyBuilder.Append(userAgent);
        }

        // Create hash for consistent key length
        var keyString = keyBuilder.ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
        return Convert.ToBase64String(hash);
    }

    private async Task ServeCachedResponseAsync(HttpContext context, CachedResponse cachedResponse)
    {
        context.Response.StatusCode = cachedResponse.StatusCode;
        context.Response.ContentType = cachedResponse.ContentType;

        // Add cache headers
        context.Response.Headers.Append("X-Cache", "HIT");
        context.Response.Headers.Append("X-Cache-Timestamp", cachedResponse.Timestamp.ToString("O"));
        context.Response.Headers.Append("Cache-Control", $"public, max-age={DefaultCacheMinutes * 60}");

        // Copy cached headers
        foreach (var header in cachedResponse.Headers)
        {
            context.Response.Headers.Append(header.Key, header.Value);
        }

        // Write cached response
        await context.Response.WriteAsync(cachedResponse.Body);
        
        _logger.LogDebug("Served cached response for {Path}", context.Request.Path);
    }

    private async Task CacheResponseAsync(HttpContext context, MemoryStream responseStream, string cacheKey)
    {
        responseStream.Position = 0;
        var responseBody = await new StreamReader(responseStream).ReadToEndAsync();

        var cachedResponse = new CachedResponse
        {
            Body = responseBody,
            StatusCode = context.Response.StatusCode,
            ContentType = context.Response.ContentType ?? "application/json",
            Timestamp = DateTime.UtcNow,
            Headers = context.Response.Headers
                .Where(h => !h.Key.StartsWith("X-") && h.Key != "Cache-Control")
                .ToDictionary(h => h.Key, h => h.Value.ToString())
        };

        // Use sliding expiration for frequently accessed responses
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(DefaultCacheMinutes),
            Size = 1, // For size-based eviction
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, cachedResponse, cacheOptions);
        
        _logger.LogDebug("Cached response for {Path} with key {CacheKey}", context.Request.Path, cacheKey);
    }
}

/// <summary>
///     Represents a cached HTTP response with metadata.
/// </summary>
public class CachedResponse
{
    public string Body { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}