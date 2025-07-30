using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Shortly.API.Middleware;

/// <summary>
///     Middleware for comprehensive performance monitoring and metrics collection.
///     Tracks response times, throughput, error rates, and provides detailed performance insights.
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, PerformanceMetrics> _endpointMetrics;

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _endpointMetrics = new Dictionary<string, PerformanceMetrics>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = GetEndpointKey(context);
        var startTime = DateTime.UtcNow;

        try
        {
            await _next(context);
            
            stopwatch.Stop();
            RecordMetrics(endpoint, stopwatch.ElapsedMilliseconds, context.Response.StatusCode, startTime, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetrics(endpoint, stopwatch.ElapsedMilliseconds, 500, startTime, false);
            
            // Re-throw to let exception middleware handle it
            throw;
        }
    }

    private string GetEndpointKey(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        return $"{method}:{path}";
    }

    private void RecordMetrics(string endpoint, long elapsedMs, int statusCode, DateTime startTime, bool success)
    {
        var cacheKey = $"metrics_{endpoint}";
        
        if (!_cache.TryGetValue(cacheKey, out PerformanceMetrics? metrics))
        {
            metrics = new PerformanceMetrics(endpoint);
        }

        metrics.RecordRequest(elapsedMs, statusCode, success, startTime);
        
        // Cache metrics for 5 minutes
        _cache.Set(cacheKey, metrics, TimeSpan.FromMinutes(5));

        // Log performance data for analysis
        if (elapsedMs > 1000) // Log slow requests
        {
            _logger.LogWarning("Slow request detected: {Endpoint} took {ElapsedMs}ms | Status: {StatusCode}",
                endpoint, elapsedMs, statusCode);
        }

        // Log performance metrics periodically
        if (metrics.TotalRequests % 100 == 0)
        {
            LogPerformanceSummary(metrics);
        }
    }

    private void LogPerformanceSummary(PerformanceMetrics metrics)
    {
        var summary = new
        {
            Endpoint = metrics.Endpoint,
            TotalRequests = metrics.TotalRequests,
            AverageResponseTime = metrics.AverageResponseTime,
            SuccessRate = metrics.SuccessRate,
            ErrorRate = metrics.ErrorRate,
            Throughput = metrics.RequestsPerMinute,
            P95ResponseTime = metrics.GetPercentileResponseTime(95),
            P99ResponseTime = metrics.GetPercentileResponseTime(99)
        };

        _logger.LogInformation("Performance summary for {Endpoint}: {@Summary}", metrics.Endpoint, summary);
    }
}

/// <summary>
///     Comprehensive performance metrics for API endpoints.
/// </summary>
public class PerformanceMetrics
{
    private readonly List<long> _responseTimes;
    private readonly List<DateTime> _requestTimes;
    private readonly object _lock = new object();

    public string Endpoint { get; }
    public long TotalRequests { get; private set; }
    public long SuccessfulRequests { get; private set; }
    public long FailedRequests { get; private set; }
    public double AverageResponseTime { get; private set; }
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
    public double ErrorRate => TotalRequests > 0 ? (double)FailedRequests / TotalRequests * 100 : 0;
    public double RequestsPerMinute { get; private set; }

    public PerformanceMetrics(string endpoint)
    {
        Endpoint = endpoint;
        _responseTimes = new List<long>();
        _requestTimes = new List<DateTime>();
    }

    public void RecordRequest(long elapsedMs, int statusCode, bool success, DateTime requestTime)
    {
        lock (_lock)
        {
            TotalRequests++;
            if (success && statusCode < 400)
            {
                SuccessfulRequests++;
            }
            else
            {
                FailedRequests++;
            }

            _responseTimes.Add(elapsedMs);
            _requestTimes.Add(requestTime);

            // Keep only last 1000 requests for memory efficiency
            if (_responseTimes.Count > 1000)
            {
                _responseTimes.RemoveAt(0);
                _requestTimes.RemoveAt(0);
            }

            // Calculate metrics
            AverageResponseTime = _responseTimes.Average();
            
            // Calculate requests per minute (last 5 minutes)
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            var recentRequests = _requestTimes.Count(t => t >= fiveMinutesAgo);
            RequestsPerMinute = recentRequests / 5.0;
        }
    }

    public long GetPercentileResponseTime(int percentile)
    {
        lock (_lock)
        {
            if (_responseTimes.Count == 0) return 0;
            
            var sortedTimes = _responseTimes.OrderBy(t => t).ToList();
            var index = (int)Math.Ceiling(percentile / 100.0 * sortedTimes.Count) - 1;
            return sortedTimes[Math.Max(0, index)];
        }
    }
}