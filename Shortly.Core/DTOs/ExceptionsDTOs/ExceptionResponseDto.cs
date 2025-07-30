using System.Text.Json.Serialization;

namespace Shortly.Core.DTOs.ExceptionsDTOs;

/// <summary>
///     Enhanced structured error response for client-side consumption with comprehensive metadata.
///     Provides detailed error information, debugging context, and performance metrics.
/// </summary>
public record ExceptionResponseDto
{
    /// <summary>
    ///     Human-readable error message suitable for end users.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    ///     Machine-readable error code for programmatic handling.
    /// </summary>
    public string ErrorCode { get; init; }

    /// <summary>
    ///     The type of exception that occurred.
    /// </summary>
    public string ExceptionType { get; init; }

    /// <summary>
    ///     HTTP status code associated with the error.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    ///     Additional error details or context information.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Details { get; init; }

    /// <summary>
    ///     Unique trace identifier for debugging and log correlation.
    /// </summary>
    public string TraceId { get; init; }

    /// <summary>
    ///     UTC timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    ///     API version that generated this response.
    /// </summary>
    public string ApiVersion { get; init; } = "1.0";

    /// <summary>
    ///     Request path that caused the error.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestPath { get; init; }

    /// <summary>
    ///     HTTP method of the request that caused the error.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestMethod { get; init; }

    /// <summary>
    ///     Performance metrics for the request (if available).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PerformanceMetrics? Performance { get; init; }

    /// <summary>
    ///     Additional debugging information (only included in development).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? DebugInfo { get; init; }

    public ExceptionResponseDto(
        string message,
        string errorCode,
        string exceptionType,
        int statusCode,
        object? details = null,
        string? traceId = null,
        string? requestPath = null,
        string? requestMethod = null,
        PerformanceMetrics? performance = null,
        object? debugInfo = null)
    {
        Message = message;
        ErrorCode = errorCode;
        ExceptionType = exceptionType;
        StatusCode = statusCode;
        Details = details;
        TraceId = traceId ?? Guid.NewGuid().ToString();
        Timestamp = DateTime.UtcNow;
        RequestPath = requestPath;
        RequestMethod = requestMethod;
        Performance = performance;
        DebugInfo = debugInfo;
    }

    /// <summary>
    ///     Creates a standardized error response with minimal required information.
    /// </summary>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="errorCode">Machine-readable error code.</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <returns>A new exception response DTO.</returns>
    public static ExceptionResponseDto Create(string message, string errorCode, int statusCode, string? traceId = null)
    {
        return new ExceptionResponseDto(
            message,
            errorCode,
            "SystemException",
            statusCode,
            null,
            traceId);
    }

    /// <summary>
    ///     Creates a standardized error response with additional context.
    /// </summary>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="errorCode">Machine-readable error code.</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="details">Additional error details.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <param name="requestPath">Request path that caused the error.</param>
    /// <param name="requestMethod">HTTP method of the request.</param>
    /// <returns>A new exception response DTO with context.</returns>
    public static ExceptionResponseDto CreateWithContext(
        string message, 
        string errorCode, 
        int statusCode, 
        object? details = null,
        string? traceId = null,
        string? requestPath = null,
        string? requestMethod = null)
    {
        return new ExceptionResponseDto(
            message,
            errorCode,
            "SystemException",
            statusCode,
            details,
            traceId,
            requestPath,
            requestMethod);
    }
}

/// <summary>
///     Performance metrics for API requests.
/// </summary>
public record PerformanceMetrics
{
    /// <summary>
    ///     Total time taken to process the request in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>
    ///     Memory usage at the time of the request in bytes.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MemoryUsageBytes { get; init; }

    /// <summary>
    ///     Database query count (if applicable).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DatabaseQueries { get; init; }

    /// <summary>
    ///     Cache hit/miss information.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CacheStatus { get; init; }

    public PerformanceMetrics(long elapsedMilliseconds, long? memoryUsageBytes = null, int? databaseQueries = null, string? cacheStatus = null)
    {
        ElapsedMilliseconds = elapsedMilliseconds;
        MemoryUsageBytes = memoryUsageBytes;
        DatabaseQueries = databaseQueries;
        CacheStatus = cacheStatus;
    }
}