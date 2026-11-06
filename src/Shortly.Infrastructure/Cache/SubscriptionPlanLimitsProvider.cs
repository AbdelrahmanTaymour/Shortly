using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Data;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Infrastructure.Cache;

internal sealed class SubscriptionPlanLimitsProvider(
    ISqlConnectionFactory sqlConnectionFactory,
    IMemoryCache cache) : ISubscriptionPlanLimitsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private const string Sql = """
                               SELECT max_links_per_month AS MaxLinksPerMonth, max_qr_codes_per_month AS MaxQrCodesPerMonth,
                                      click_data_retention_days AS ClickDataRetentionDays,
                                      max_members_per_organization AS MaxMembersPerOrganization, max_organizations AS MaxOrganizations,
                                      link_analysis AS LinkAnalysis,
                                      bulk_creation AS BulkCreation, link_protection AS LinkProtection,
                                      custom_short_code AS CustomShortCode, campaign_tracking AS CampaignTracking,
                                      geo_device_tracking AS GeoDeviceTracking
                               FROM subscription_plans
                               WHERE id = @PlanId
                               """;

    public async Task<SubscriptionPlanLimits> GetLimitsAsync(enSubscriptionPlan planId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"subscription-plan:{(byte)planId}:limits";
        if (cache.TryGetValue(cacheKey, out SubscriptionPlanLimits? cached) && cached is not null)
            return cached;

        using var connection = sqlConnectionFactory.CreateConnection();
        var command = new CommandDefinition(Sql, new { PlanId = (byte)planId }, cancellationToken: cancellationToken);
        var limits = await connection.QuerySingleOrDefaultAsync<SubscriptionPlanLimits>(command);

        if (limits is null)
        {
            throw new InvalidOperationException(
                $"Subscription plan '{planId}' has no matching row in subscription_plans. Check migrations/seed data.");
        }

        cache.Set(cacheKey, limits, CacheDuration);
        return limits;
    }
}