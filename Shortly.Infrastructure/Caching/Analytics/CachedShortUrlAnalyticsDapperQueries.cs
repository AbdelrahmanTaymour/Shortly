using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.ShortUrls.DTOs;

namespace Shortly.Infrastructure.Caching.Analytics;

// ── Decorator 2: Command Side ────────────────────────────────────────────────
public sealed class CachedShortUrlAnalyticsDapperQueries(
    IShortUrlAnalyticsDapperQueries inner,
    IMemoryCache cache,
    ILogger<CachedShortUrlAnalyticsDapperQueries> logger)
    : IShortUrlAnalyticsDapperQueries
{
    private static readonly TimeSpan SummaryTtl = TimeSpan.FromMinutes(5);

    public async Task<UserAnalyticsSummary> GetUserAnalyticsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"{AnalyticsCacheConstants.PfxUser}:{userId}";
        if (cache.TryGetValue(key, out UserAnalyticsSummary? hit) && hit is not null) return hit;
 
        var result = await inner.GetUserAnalyticsAsync(userId, cancellationToken);
        cache.Set(key, result, AnalyticsCacheConstants.Opts(SummaryTtl));
        return result;
    }
 
    public async Task<OrganizationAnalyticsSummary> GetOrganizationAnalyticsAsync(
        Guid organizationId, CancellationToken cancellationToken = default)
    {
        var key = $"{AnalyticsCacheConstants.PfxOrg}:{organizationId}";
        if (cache.TryGetValue(key, out OrganizationAnalyticsSummary? hit) && hit is not null) return hit;
 
        var result = await inner.GetOrganizationAnalyticsAsync(organizationId, cancellationToken);
        cache.Set(key, result, AnalyticsCacheConstants.Opts(SummaryTtl));
        return result;
    }
}