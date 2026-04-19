using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shortly.Core.Analytics.CacheContracts;

namespace Shortly.Infrastructure.Caching.Analytics;

/// <summary>
///     Hybrid two-tier caching service (L1: IMemoryCache, L2: Redis).
/// </summary>
public class CachedStatisticsService(
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    ILogger<CachedStatisticsService> logger)
    : ICachedStatisticsService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan L1Ttl = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private long _cacheHits;
    private long _cacheMisses;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();
    private readonly ConcurrentDictionary<long, ConcurrentBag<string>> _urlKeys = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<string>> _userKeys = new();

    private volatile bool _redisCircuitOpen;
    private long _redisOpenedAtTicks;
    private static readonly TimeSpan RedisRetryWindow = TimeSpan.FromSeconds(30);

    private bool IsRedisAvailable()
    {
        if (!_redisCircuitOpen) return true;
        var elapsed = TimeSpan.FromTicks(Environment.TickCount64 - Interlocked.Read(ref _redisOpenedAtTicks));
        if (elapsed < RedisRetryWindow) return false;
        _redisCircuitOpen = false; // half-open: let one probe through
        return true;
    }

    private void TripRedisCircuit(Exception ex, string key)
    {
        logger.LogWarning(ex, "Redis unavailable on key {Key}. Circuit open for {Window}s.", key,
            (int)RedisRetryWindow.TotalSeconds);
        _redisCircuitOpen = true;
        Interlocked.Exchange(ref _redisOpenedAtTicks, Environment.TickCount64);
    }

    #region Generic Slice Cache

    /// <inheritdoc />
    public async Task<T> GetOrSetUrlMetricAsync<T>(
        string metricName,
        long shortUrlId,
        DateTime? startDate,
        DateTime? endDate,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var key = BuildMetricKey(metricName, shortUrlId, startDate, endDate);
        var result = await GetOrSetAsync(key, factory, ttl ?? DefaultTtl, cancellationToken);
        // Register in the URL key bag so InvalidateUrlStatisticsAsync evicts this slice too
        TrackUrlKey(shortUrlId, key);
        return result;
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetUserMetricAsync<T>(
        string metricName,
        Guid userId,
        DateTime? startDate,
        DateTime? endDate,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var key = BuildUserMetricKey(metricName, userId, startDate, endDate);
        var result = await GetOrSetAsync(key, factory, ttl ?? DefaultTtl, cancellationToken);
        // Register in a User key bag so InvalidateUserStatisticsAsync evicts this slice too
        TrackUserKey(userId, key);
        return result;
    }

    #endregion

    #region Core Cache Engine

    private async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken) where T : class
    {
        var fast = await GetFromCacheAsync<T>(key, cancellationToken);
        if (fast is not null) return fast;

        var sem = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring a lock
            var afterLock = await GetFromCacheAsync<T>(key, cancellationToken);
            if (afterLock is not null) return afterLock;

            logger.LogDebug("Cache MISS (computing): {Key}", key);
            var computed = await factory(cancellationToken);
            await SetInCacheAsync(key, computed, ttl, cancellationToken);
            return computed;
        }
        finally
        {
            sem.Release();
        }
    }

    private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        if (memoryCache.TryGetValue<T>(key, out var l1Hit))
        {
            Interlocked.Increment(ref _cacheHits);
            logger.LogTrace("L1 HIT: {Key}", key);
            return l1Hit;
        }

        // Skip Redis entirely while the circuit is open — avoids 5 000 ms SDK timeout
        if (!IsRedisAvailable())
        {
            Interlocked.Increment(ref _cacheMisses);
            return null;
        }

        string? serialized;
        try
        {
            serialized = await distributedCache.GetStringAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            TripRedisCircuit(ex, key);
            Interlocked.Increment(ref _cacheMisses);
            return null;
        }

        if (string.IsNullOrEmpty(serialized))
        {
            Interlocked.Increment(ref _cacheMisses);
            return null;
        }

        try
        {
            var item = JsonSerializer.Deserialize<T>(serialized, JsonOptions);
            if (item is not null)
            {
                memoryCache.Set(key, item, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = L1Ttl, Size = 1
                });
                Interlocked.Increment(ref _cacheHits);
                logger.LogTrace("L2 HIT (L1 warmed): {Key}", key);
                return item;
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Corrupted cache value for key {Key}. Evicting.", key);
            try
            {
                await distributedCache.RemoveAsync(key, cancellationToken);
            }
            catch
            {
                /* best-effort */
            }
        }

        Interlocked.Increment(ref _cacheMisses);
        return null;
    }

    private async Task SetInCacheAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class
    {
        memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl < L1Ttl ? ttl : L1Ttl, Size = 1
        });

        try
        {
            var serialized = JsonSerializer.Serialize(value, JsonOptions);
            await distributedCache.SetStringAsync(key, serialized,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                cancellationToken);
        }
        catch (Exception ex)
        {
            TripRedisCircuit(ex, key);
        }

        logger.LogDebug("Cached: {Key}, TTL: {Ttl}", key, ttl);
    }

    #endregion

    #region Key Registry Helpers

    private void TrackUrlKey(long urlId, string key)
    {
        _urlKeys.GetOrAdd(urlId, _ => []).Add(key);
    }

    private void TrackUserKey(Guid userId, string key)
    {
        _userKeys.GetOrAdd(userId, _ => []).Add(key);
    }

    #endregion

    #region Key Builders

    private static string BuildMetricKey(string metricType, long shortUrlId, DateTime? startDate, DateTime? endDate)
    {
        return $"stats:metric:{metricType}:{shortUrlId}:{D(startDate)}:{D(endDate)}";
    }

    private static string BuildUserMetricKey(string metricType, Guid userId, DateTime? startDate, DateTime? endDate)
    {
        return $"stats:umetric:{metricType}:{userId}:{D(startDate)}:{D(endDate)}";
    }

    private static string D(DateTime? dt)
    {
        return dt?.ToString("yyyy-MM-dd") ?? "all";
    }

    #endregion
}