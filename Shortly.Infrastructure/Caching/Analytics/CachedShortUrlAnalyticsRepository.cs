using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shortly.Domain.Entities;
using Shortly.Domain.RepositoryContract.Analytics;

namespace Shortly.Infrastructure.Caching.Analytics;

// ── Decorator 1: Query Side ──────────────────────────────────────────────────
public sealed class CachedShortUrlAnalyticsRepository(
    IShortUrlAnalyticsRepository inner,
    IMemoryCache cache,
    ILogger<CachedShortUrlAnalyticsRepository> logger)
    : IShortUrlAnalyticsRepository
{
    private static readonly TimeSpan CountTtl       = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ClicksTtl      = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PopularUrlsTtl = TimeSpan.FromMinutes(15);

    public async Task<int> GetTotalCountAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var key = $"{AnalyticsCacheConstants.PfxCount}:{activeOnly}";
        if (cache.TryGetValue(key, out int hit)) return hit;
 
        var result = await inner.GetTotalCountAsync(activeOnly, cancellationToken);
        cache.Set(key, result, AnalyticsCacheConstants.Opts(CountTtl));
        return result;
    }
 
    public async Task<int> GetTotalClicksAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        var key = $"{AnalyticsCacheConstants.PfxClicks}:{shortUrlId}";
        if (cache.TryGetValue(key, out int hit)) return hit;
 
        var result = await inner.GetTotalClicksAsync(shortUrlId, cancellationToken);
        cache.Set(key, result, AnalyticsCacheConstants.Opts(ClicksTtl));
        return result;
    }
 
    public async Task<IEnumerable<ShortUrl>> GetMostPopularUrlAsync(
        int topCount = 10, TimeSpan? timeframe = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var key = $"{AnalyticsCacheConstants.PfxPopular}:{topCount}:{timeframe?.Ticks ?? -1}:{userId?.ToString() ?? "all"}";
        if (cache.TryGetValue(key, out IEnumerable<ShortUrl>? hit) && hit is not null) return hit;
 
        var result = await inner.GetMostPopularUrlAsync(topCount, timeframe, userId, cancellationToken);
        var materialized = result.ToList();
        cache.Set<IEnumerable<ShortUrl>>(key, materialized, AnalyticsCacheConstants.Opts(PopularUrlsTtl));
        return materialized;
    }
 
    public Task<IEnumerable<ShortUrl>> GetApproachingLimitAsync(
        double warningThreshold = 0.8, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        => inner.GetApproachingLimitAsync(warningThreshold, pageNumber, pageSize, cancellationToken);
        
    public void InvalidateUrl(long shortUrlId)
    {
        cache.Remove($"{AnalyticsCacheConstants.PfxClicks}:{shortUrlId}");
        cache.Remove($"{AnalyticsCacheConstants.PfxCount}:True");
        cache.Remove($"{AnalyticsCacheConstants.PfxCount}:False");
        logger.LogDebug("L1 analytics cache invalidated for URL {ShortUrlId}", shortUrlId);
    }
}