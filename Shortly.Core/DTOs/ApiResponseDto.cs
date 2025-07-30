using System.Text.Json.Serialization;

namespace Shortly.Core.DTOs;

/// <summary>
///     Standardized API response wrapper for consistent response structure across all endpoints.
///     Provides unified format for both success and error responses with metadata.
/// </summary>
/// <typeparam name="T">Type of the response data.</typeparam>
public record ApiResponseDto<T>
{
    /// <summary>
    ///     Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Human-readable message describing the result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    ///     The actual response data (null for error responses).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    /// <summary>
    ///     Error details (null for successful responses).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Error { get; init; }

    /// <summary>
    ///     Unique trace identifier for debugging and log correlation.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    ///     UTC timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    ///     API version that generated this response.
    /// </summary>
    public string ApiVersion { get; init; } = "1.0";

    /// <summary>
    ///     Performance metrics for the request (if available).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PerformanceMetrics? Performance { get; init; }

    /// <summary>
    ///     Pagination information (if applicable).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationInfo? Pagination { get; init; }

    /// <summary>
    ///     Creates a successful API response.
    /// </summary>
    /// <param name="data">Response data.</param>
    /// <param name="message">Success message.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <param name="performance">Performance metrics.</param>
    /// <param name="pagination">Pagination information.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponseDto<T> Success(
        T data, 
        string message = "Operation completed successfully",
        string? traceId = null,
        PerformanceMetrics? performance = null,
        PaginationInfo? pagination = null)
    {
        return new ApiResponseDto<T>
        {
            Success = true,
            Message = message,
            Data = data,
            TraceId = traceId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Performance = performance,
            Pagination = pagination
        };
    }

    /// <summary>
    ///     Creates an error API response.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="error">Error details.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <param name="performance">Performance metrics.</param>
    /// <returns>An error API response.</returns>
    public static ApiResponseDto<T> Error(
        string message, 
        object? error = null,
        string? traceId = null,
        PerformanceMetrics? performance = null)
    {
        return new ApiResponseDto<T>
        {
            Success = false,
            Message = message,
            Error = error,
            TraceId = traceId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Performance = performance
        };
    }

    /// <summary>
    ///     Creates a successful API response with pagination.
    /// </summary>
    /// <param name="data">Response data.</param>
    /// <param name="pagination">Pagination information.</param>
    /// <param name="message">Success message.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <param name="performance">Performance metrics.</param>
    /// <returns>A successful API response with pagination.</returns>
    public static ApiResponseDto<T> SuccessWithPagination(
        T data,
        PaginationInfo pagination,
        string message = "Operation completed successfully",
        string? traceId = null,
        PerformanceMetrics? performance = null)
    {
        return new ApiResponseDto<T>
        {
            Success = true,
            Message = message,
            Data = data,
            TraceId = traceId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Performance = performance,
            Pagination = pagination
        };
    }
}

/// <summary>
///     Non-generic API response for simple success/error responses without data.
/// </summary>
public record ApiResponseDto : ApiResponseDto<object>
{
    /// <summary>
    ///     Creates a successful API response without data.
    /// </summary>
    /// <param name="message">Success message.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <param name="performance">Performance metrics.</param>
    /// <returns>A successful API response.</returns>
    public static ApiResponseDto Success(
        string message = "Operation completed successfully",
        string? traceId = null,
        PerformanceMetrics? performance = null)
    {
        return new ApiResponseDto
        {
            Success = true,
            Message = message,
            TraceId = traceId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Performance = performance
        };
    }

    /// <summary>
    ///     Creates an error API response without data.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="error">Error details.</param>
    /// <param name="traceId">Unique trace identifier.</param>
    /// <param name="performance">Performance metrics.</param>
    /// <returns>An error API response.</returns>
    public static ApiResponseDto Error(
        string message,
        object? error = null,
        string? traceId = null,
        PerformanceMetrics? performance = null)
    {
        return new ApiResponseDto
        {
            Success = false,
            Message = message,
            Error = error,
            TraceId = traceId ?? Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Performance = performance
        };
    }
}

/// <summary>
///     Pagination information for paginated responses.
/// </summary>
public record PaginationInfo
{
    /// <summary>
    ///     Current page number (1-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    ///     Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    ///     Total number of items across all pages.
    /// </summary>
    public long TotalItems { get; init; }

    /// <summary>
    ///     Total number of pages.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    ///     Whether there is a next page.
    /// </summary>
    public bool HasNextPage { get; init; }

    /// <summary>
    ///     Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage { get; init; }

    public PaginationInfo(int page, int pageSize, long totalItems)
    {
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        HasNextPage = page < TotalPages;
        HasPreviousPage = page > 1;
    }
}