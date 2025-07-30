using System.Net;
using System.Text.Json;
using FluentValidation;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.Exceptions.Base;
using ClientErrors = Shortly.Core.Exceptions.ClientErrors;

namespace Shortly.API.Middleware;

/// <summary>
///     Enhanced middleware for centralized exception handling with performance optimizations.
///     Catches and processes all unhandled exceptions, converting them into standardized API responses.
///     Features include response caching, structured logging, and performance monitoring.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<Type, (int StatusCode, string ErrorCode)> _exceptionMapping;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExceptionHandlingMiddleware" /> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance for recording exception details.</param>
    /// <param name="cache">Memory cache for response optimization.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        
        // Optimized JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // Pre-configured exception mappings for faster lookups
        _exceptionMapping = new Dictionary<Type, (int StatusCode, string ErrorCode)>
        {
            { typeof(ArgumentNullException), (400, "MissingParameter") },
            { typeof(ArgumentException), (400, "ArgumentError") },
            { typeof(UnauthorizedAccessException), (403, "AccessDenied") },
            { typeof(TimeoutException), (408, "Timeout") },
            { typeof(TaskCanceledException), (408, "OperationCancelled") },
            { typeof(InvalidOperationException), (400, "InvalidOperation") },
            { typeof(NotSupportedException), (400, "NotSupported") }
        };
    }

    /// <summary>
    ///     Processes an HTTP request and handles any exceptions that occur with performance monitoring.
    /// </summary>
    /// <param name="httpContext">The context for the current HTTP request.</param>
    public async Task Invoke(HttpContext httpContext)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var traceId = httpContext.TraceIdentifier;

        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await HandleExceptionAsync(httpContext, ex, stopwatch.ElapsedMilliseconds, traceId);
        }
    }

    /// <summary>
    ///     Enhanced exception handling with performance monitoring and structured logging.
    /// </summary>
    /// <param name="context">The context for the current HTTP request.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="elapsedMs">Time elapsed before exception occurred.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception, long elapsedMs, string traceId)
    {
        // Handle custom application exceptions first (most common)
        if (exception is BaseApplicationException appException)
        {
            await HandleApplicationExceptionAsync(context, appException, traceId, elapsedMs);
            return;
        }

        // Handle FluentValidation exceptions
        if (exception is ValidationException fluentValidationEx)
        {
            var customValidationEx = new ClientErrors.ValidationException(fluentValidationEx);
            await HandleApplicationExceptionAsync(context, customValidationEx, traceId, elapsedMs);
            return;
        }

        // Use pre-configured mappings for faster lookups
        var response = GetCachedOrCreateErrorResponse(exception, traceId, elapsedMs);

        // Enhanced structured logging
        LogExceptionWithContext(exception, response.StatusCode, traceId, elapsedMs, context);

        // Send optimized response
        await SendOptimizedErrorResponseAsync(context, response);
    }

    /// <summary>
    ///     Handles application-specific exceptions with enhanced logging and performance tracking.
    /// </summary>
    /// <param name="context">The context for the current HTTP request.</param>
    /// <param name="exception">The application-specific exception to handle.</param>
    /// <param name="traceId">The unique identifier for tracing the request.</param>
    /// <param name="elapsedMs">Time elapsed before exception occurred.</param>
    private async Task HandleApplicationExceptionAsync(HttpContext context, BaseApplicationException exception,
        string traceId, long elapsedMs)
    {
        ExceptionResponseDto response;

        // Special handling for validation exceptions with detailed error mapping
        if (exception is ClientErrors.ValidationException validationEx)
        {
            response = new ValidationErrorResponseDto(
                validationEx.Message,
                validationEx.ValidationErrors,
                traceId);

            // Enhanced validation logging with field-level details
            _logger.LogWarning(
                "Validation failed: {Message} | TraceId: {TraceId} | Fields: {FieldCount} | Elapsed: {ElapsedMs}ms | Details: {@Details}",
                validationEx.Message, 
                traceId, 
                validationEx.ValidationErrors.Count,
                elapsedMs,
                validationEx.ValidationErrors);
        }
        else
        {
            response = new ExceptionResponseDto(
                exception.Message,
                exception.ErrorCode,
                exception.GetType().Name,
                (int)exception.StatusCode,
                exception.Details,
                traceId);

            // Enhanced logging based on severity with performance context
            if ((int)exception.StatusCode >= 500)
            {
                _logger.LogError(exception, 
                    "Server error occurred: {Message} | TraceId: {TraceId} | Elapsed: {ElapsedMs}ms | UserAgent: {UserAgent}",
                    exception.Message, traceId, elapsedMs, context.Request.Headers.UserAgent.ToString());
            }
            else
            {
                _logger.LogWarning(
                    "Client error occurred: {Message} | TraceId: {TraceId} | StatusCode: {StatusCode} | Elapsed: {ElapsedMs}ms | IP: {ClientIP}",
                    exception.Message, traceId, (int)exception.StatusCode, elapsedMs, 
                    context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
            }
        }

        await SendOptimizedErrorResponseAsync(context, response);
    }

    /// <summary>
    ///     Creates or retrieves cached error responses for better performance.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <param name="elapsedMs">Time elapsed before exception occurred.</param>
    /// <returns>Standardized exception response DTO.</returns>
    private ExceptionResponseDto GetCachedOrCreateErrorResponse(Exception exception, string traceId, long elapsedMs)
    {
        var exceptionType = exception.GetType();
        
        // Try to get from cache first
        var cacheKey = $"error_response_{exceptionType.Name}_{exception.Message}";
        if (_cache.TryGetValue(cacheKey, out ExceptionResponseDto? cachedResponse))
        {
            // Clone cached response with new trace ID and timestamp
            return new ExceptionResponseDto(
                cachedResponse.Message,
                cachedResponse.ErrorCode,
                cachedResponse.ExceptionType,
                cachedResponse.StatusCode,
                cachedResponse.Details,
                traceId);
        }

        // Create new response
        var response = CreateErrorResponse(exception, traceId);
        
        // Cache common error responses (avoid caching sensitive or unique errors)
        if (ShouldCacheError(exception))
        {
            _cache.Set(cacheKey, response, TimeSpan.FromMinutes(30));
        }

        return response;
    }

    /// <summary>
    ///     Determines if an error response should be cached based on exception type and content.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>True if the error should be cached, false otherwise.</returns>
    private static bool ShouldCacheError(Exception exception)
    {
        // Cache common system exceptions, avoid caching application-specific or sensitive errors
        return exception switch
        {
            ArgumentNullException => true,
            ArgumentException => true,
            UnauthorizedAccessException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            _ => false
        };
    }

    /// <summary>
    ///     Creates a standardized error response with enhanced error categorization.
    /// </summary>
    /// <param name="exception">The exception to create response for.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <returns>A standardized exception response DTO.</returns>
    private ExceptionResponseDto CreateErrorResponse(Exception exception, string traceId)
    {
        var exceptionType = exception.GetType();
        
        // Use pre-configured mappings for faster lookups
        if (_exceptionMapping.TryGetValue(exceptionType, out var mapping))
        {
            return new ExceptionResponseDto(
                exception.Message,
                mapping.ErrorCode,
                exceptionType.Name,
                mapping.StatusCode,
                null,
                traceId);
        }

        // Fallback for unmapped exceptions
        return new ExceptionResponseDto(
            "An unexpected error occurred.",
            "InternalError",
            "SystemException",
            500,
            null,
            traceId);
    }

    /// <summary>
    ///     Enhanced structured logging with context information and performance metrics.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="statusCode">The HTTP status code associated with the error.</param>
    /// <param name="traceId">The unique identifier for tracing the request.</param>
    /// <param name="elapsedMs">Time elapsed before exception occurred.</param>
    /// <param name="context">HTTP context for additional information.</param>
    private void LogExceptionWithContext(Exception exception, int statusCode, string traceId, long elapsedMs, HttpContext context)
    {
        var logData = new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["StatusCode"] = statusCode,
            ["ElapsedMs"] = elapsedMs,
            ["RequestPath"] = context.Request.Path.ToString(),
            ["RequestMethod"] = context.Request.Method,
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["ClientIP"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            ["ExceptionType"] = exception.GetType().Name,
            ["ExceptionMessage"] = exception.Message
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, 
                "Unhandled server exception: {Message} | Context: {@Context}",
                exception.Message, logData);
        }
        else
        {
            _logger.LogWarning(
                "Client exception: {Message} | Context: {@Context}",
                exception.Message, logData);
        }
    }

    /// <summary>
    ///     Sends optimized error response with compression and caching headers.
    /// </summary>
    /// <param name="context">The context for the current HTTP request.</param>
    /// <param name="response">The error response to send.</param>
    private async Task SendOptimizedErrorResponseAsync(HttpContext context, ExceptionResponseDto response)
    {
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";
        
        // Add performance and debugging headers
        context.Response.Headers.Append("X-Error-Code", response.ErrorCode);
        context.Response.Headers.Append("X-Exception-Type", response.ExceptionType);
        context.Response.Headers.Append("X-Trace-Id", response.TraceId);
        
        // Cache control for error responses (short cache for client errors, no cache for server errors)
        if (response.StatusCode < 500)
        {
            context.Response.Headers.Append("Cache-Control", "private, max-age=60");
        }
        else
        {
            context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        }

        var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}