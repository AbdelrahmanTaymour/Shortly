namespace Shortly.Core.Analytics.CacheContracts;

/// <summary>
///     Hybrid two-tier caching service for statistics (L1: IMemoryCache, L2: Redis).
/// </summary>
public interface ICachedStatisticsService
{
    /// <summary>
    ///     Generic stampede-safe GetOrSet for any metric slice type.
    /// </summary>
    Task<T> GetOrSetUrlMetricAsync<T>(
        string metricName,
        long shortUrlId,
        DateTime? startDate,
        DateTime? endDate,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <inheritdoc cref="GetOrSetUrlMetricAsync{T}" />
    Task<T> GetOrSetUserMetricAsync<T>(
        string metricName,
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class;
}