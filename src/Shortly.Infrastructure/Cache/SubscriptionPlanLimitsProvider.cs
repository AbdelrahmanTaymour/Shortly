using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Data;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.Cache;

internal sealed class SubscriptionPlanLimitsProvider(
    ISqlConnectionFactory sqlConnectionFactory,
    IMemoryCache cache) : ISubscriptionPlanLimitsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public async Task<int> GetMaxLinksPerMonthAsync(enSubscriptionPlan planId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"subscription-plan:{(byte)planId}:max-links";
        if (cache.TryGetValue(cacheKey, out int cached)) return cached;

        using var connection = sqlConnectionFactory.CreateConnection();
        var maxLinks = await connection.QuerySingleOrDefaultAsync<int?>(
            "SELECT max_links_per_month FROM subscription_plans WHERE id = @PlanId", new { PlanId = (byte)planId });

        var value = maxLinks ?? -1;
        cache.Set(cacheKey, value, CacheDuration);
        return value;
    }
}