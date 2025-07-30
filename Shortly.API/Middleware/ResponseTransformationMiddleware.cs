using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Shortly.API.Middleware;

/// <summary>
///     Middleware for transforming API responses into standardized format with performance metrics.
///     Centralizes response formatting, performance monitoring, and error handling.
/// </summary>
public class ResponseTransformationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTransformationMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public ResponseTransformationMiddleware(RequestDelegate next, ILogger<ResponseTransformationMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = context.TraceIdentifier;

        // Capture the original response stream
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Add performance headers
            context.Response.Headers.Append("X-Response-Time", $"{elapsedMs}ms");
            context.Response.Headers.Append("X-Trace-Id", traceId);

            // Transform response based on status code and content type
            await TransformResponseAsync(context, memoryStream, elapsedMs, traceId);

            // Copy the transformed response back to the original stream
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task TransformResponseAsync(HttpContext context, MemoryStream responseStream, long elapsedMs, string traceId)
    {
        var statusCode = context.Response.StatusCode;
        var contentType = context.Response.ContentType;

        // Don't transform certain response types
        if (ShouldSkipTransformation(context, contentType))
        {
            return;
        }

        // Read the original response
        responseStream.Position = 0;
        var responseBody = await new StreamReader(responseStream).ReadToEndAsync();

        // Transform based on status code
        var transformedResponse = statusCode switch
        {
            >= 200 and < 300 => TransformSuccessResponse(responseBody, elapsedMs, traceId, context),
            >= 400 and < 500 => TransformClientErrorResponse(responseBody, statusCode, elapsedMs, traceId, context),
            >= 500 => TransformServerErrorResponse(responseBody, statusCode, elapsedMs, traceId, context),
            _ => responseBody // Don't transform other status codes
        };

        // Write the transformed response
        responseStream.SetLength(0);
        responseStream.Position = 0;
        var responseBytes = System.Text.Encoding.UTF8.GetBytes(transformedResponse);
        await responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
    }

    private bool ShouldSkipTransformation(HttpContext context, string? contentType)
    {
        // Skip transformation for certain content types
        if (string.IsNullOrEmpty(contentType)) return true;
        
        var skipContentTypes = new[]
        {
            "text/html",
            "application/pdf",
            "image/",
            "audio/",
            "video/"
        };

        if (skipContentTypes.Any(ct => contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Skip for redirects and file downloads
        if (context.Response.StatusCode == 301 || context.Response.StatusCode == 302)
            return true;

        // Skip for health checks and metrics endpoints
        var path = context.Request.Path.Value?.ToLower();
        if (path?.Contains("/health") == true || path?.Contains("/metrics") == true)
            return true;

        return false;
    }

    private string TransformSuccessResponse(string responseBody, long elapsedMs, string traceId, HttpContext context)
    {
        try
        {
            // Try to parse as JSON and wrap it
            if (IsJsonResponse(responseBody))
            {
                var data = JsonSerializer.Deserialize<object>(responseBody, _jsonOptions);
                var performance = new PerformanceMetrics(elapsedMs);
                
                var apiResponse = new ApiResponseDto<object>
                {
                    Success = true,
                    Message = GetSuccessMessage(context.Request.Path.Value, context.Request.Method),
                    Data = data,
                    TraceId = traceId,
                    Timestamp = DateTime.UtcNow,
                    ApiVersion = "1.0",
                    Performance = performance
                };

                return JsonSerializer.Serialize(apiResponse, _jsonOptions);
            }

            // For non-JSON responses, create a simple success response
            var simpleResponse = new ApiResponseDto
            {
                Success = true,
                Message = GetSuccessMessage(context.Request.Path.Value, context.Request.Method),
                TraceId = traceId,
                Timestamp = DateTime.UtcNow,
                ApiVersion = "1.0",
                Performance = new PerformanceMetrics(elapsedMs)
            };

            return JsonSerializer.Serialize(simpleResponse, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to transform success response for {Path}", context.Request.Path);
            return responseBody; // Return original response if transformation fails
        }
    }

    private string TransformClientErrorResponse(string responseBody, int statusCode, long elapsedMs, string traceId, HttpContext context)
    {
        try
        {
            // Try to parse existing error response
            if (IsJsonResponse(responseBody))
            {
                var existingError = JsonSerializer.Deserialize<object>(responseBody, _jsonOptions);
                var performance = new PerformanceMetrics(elapsedMs);

                var apiResponse = new ApiResponseDto<object>
                {
                    Success = false,
                    Message = GetErrorMessage(statusCode, context.Request.Path.Value),
                    Error = existingError,
                    TraceId = traceId,
                    Timestamp = DateTime.UtcNow,
                    ApiVersion = "1.0",
                    Performance = performance
                };

                return JsonSerializer.Serialize(apiResponse, _jsonOptions);
            }

            // Create error response for non-JSON responses
            var errorResponse = new ApiResponseDto
            {
                Success = false,
                Message = GetErrorMessage(statusCode, context.Request.Path.Value),
                Error = new { StatusCode = statusCode, Details = responseBody },
                TraceId = traceId,
                Timestamp = DateTime.UtcNow,
                ApiVersion = "1.0",
                Performance = new PerformanceMetrics(elapsedMs)
            };

            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to transform client error response for {Path}", context.Request.Path);
            return responseBody;
        }
    }

    private string TransformServerErrorResponse(string responseBody, int statusCode, long elapsedMs, string traceId, HttpContext context)
    {
        try
        {
            var performance = new PerformanceMetrics(elapsedMs);
            var errorDetails = IsJsonResponse(responseBody) 
                ? JsonSerializer.Deserialize<object>(responseBody, _jsonOptions)
                : new { Message = responseBody };

            var apiResponse = new ApiResponseDto<object>
            {
                Success = false,
                Message = "An internal server error occurred. Please try again later.",
                Error = errorDetails,
                TraceId = traceId,
                Timestamp = DateTime.UtcNow,
                ApiVersion = "1.0",
                Performance = performance
            };

            return JsonSerializer.Serialize(apiResponse, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to transform server error response for {Path}", context.Request.Path);
            return responseBody;
        }
    }

    private bool IsJsonResponse(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return false;
        
        responseBody = responseBody.Trim();
        return (responseBody.StartsWith("{") && responseBody.EndsWith("}")) ||
               (responseBody.StartsWith("[") && responseBody.EndsWith("]"));
    }

    private string GetSuccessMessage(string? path, string method)
    {
        if (string.IsNullOrEmpty(path)) return "Operation completed successfully";

        return (path.ToLower(), method.ToUpper()) switch
        {
            (var p, "GET") when p.Contains("getall") => "Short URLs retrieved successfully",
            (var p, "POST") when p.Contains("create") => "Short URL created successfully",
            (var p, "PUT") when p.Contains("update") => "Short URL updated successfully",
            (var p, "DELETE") when p.Contains("delete") => "Short URL deleted successfully",
            (var p, "GET") when p.Contains("stats") => "Statistics retrieved successfully",
            _ => "Operation completed successfully"
        };
    }

    private string GetErrorMessage(int statusCode, string? path)
    {
        return statusCode switch
        {
            400 => "Invalid request data provided",
            401 => "Authentication required",
            403 => "Access denied",
            404 => "Resource not found",
            409 => "Resource conflict",
            422 => "Validation failed",
            _ => "An error occurred while processing your request"
        };
    }
}

/// <summary>
///     Performance metrics for API requests.
/// </summary>
public class PerformanceMetrics
{
    public long ElapsedMilliseconds { get; init; }
    public DateTime Timestamp { get; init; }

    public PerformanceMetrics(long elapsedMilliseconds)
    {
        ElapsedMilliseconds = elapsedMilliseconds;
        Timestamp = DateTime.UtcNow;
    }
}