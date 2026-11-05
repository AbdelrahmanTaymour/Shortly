using Dapper;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Cache;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Data;
using Shortly.Domain.Abstractions;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.Analytics;

/// <summary>Defines the minimum feature tier required for an analytics request.</summary>
internal enum AnalyticsFeature
{
    /// <summary>Basic link management data (summary, approaching-limit): available on all plans.</summary>
    None,

    /// <summary>Click-event analytics: requires the LinkAnalysis feature.</summary>
    LinkAnalysis,

    /// <summary>Geography and device analytics: requires both LinkAnalysis and GeoDeviceTracking.</summary>
    GeoDeviceAnalytics
}

/// <summary>
/// Resolves and secures the analytics scope from the authenticated caller, enforcing owner plan limits and retention windows.
/// </summary>
internal sealed class AnalyticsScopeResolver(
    IUserContext userContext,
    IOrganizationAuthorizationService organizationAuthorization,
    ISqlConnectionFactory sqlConnectionFactory,
    ISubscriptionPlanLimitsProvider planLimitsProvider,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result<AnalyticsScope>> ResolveAsync(
        long? shortUrlId, DateTime? from, DateTime? to, AnalyticsFeature feature, CancellationToken cancellationToken)
    {
        AnalyticsScope scope;
        enSubscriptionPlan ownerPlan;

        if (shortUrlId is { } id)
        {
            var owner = await FindAccessibleOwnerAsync(id, cancellationToken);
            
            // Return 404 instead of 403 to prevent resource existence enumeration attacks
            if (owner is null) return Result.Failure<AnalyticsScope>(ShortUrlErrors.NotFound);

            scope = new AnalyticsScope(id, null, null, from, to);
            ownerPlan = owner.Plan;
        }
        else
        {
            var membership = await organizationAuthorization.ResolveClaimedMembershipAsync(cancellationToken);
            if (membership.IsFailure) return Result.Failure<AnalyticsScope>(membership.Error);

            if (membership.Value.Member is { } member)
            {
                scope = new AnalyticsScope(null, null, member.OrganizationId, from, to);
                ownerPlan = feature == AnalyticsFeature.None
                    ? enSubscriptionPlan.Free
                    : await QueryPlanAsync("organizations", member.OrganizationId, cancellationToken);
            }
            else
            {
                scope = new AnalyticsScope(null, userContext.UserId, null, from, to);
                ownerPlan = feature == AnalyticsFeature.None
                    ? enSubscriptionPlan.Free
                    : await QueryPlanAsync("users", userContext.UserId, cancellationToken);
            }
        }

        // Basic data and admin requests bypass subscription plan feature limits
        if (feature == AnalyticsFeature.None || userContext.IsAdmin)
            return scope with { IncludeGeo = true, IncludeCampaigns = true };

        var limits = await planLimitsProvider.GetLimitsAsync(ownerPlan, cancellationToken);

        if (!limits.LinkAnalysis) return Result.Failure<AnalyticsScope>(ShortUrlErrors.LinkAnalysisNotAllowed);
        if (feature == AnalyticsFeature.GeoDeviceAnalytics && !limits.GeoDeviceTracking)
            return Result.Failure<AnalyticsScope>(ShortUrlErrors.GeoDeviceTrackingNotAllowed);

        return scope with
        {
            // Clamp request date to the plan's historical click data retention window
            From = AnalyticsRetention.ClampFrom(from, limits.ClickDataRetentionDays, dateTimeProvider.UtcNow),
            IncludeGeo = limits.GeoDeviceTracking,
            IncludeCampaigns = limits.CampaignTracking
        };
    }

    private sealed record AccessibleOwner(enSubscriptionPlan Plan);

    private async Task<AccessibleOwner?> FindAccessibleOwnerAsync(long shortUrlId, CancellationToken cancellationToken)
    {
        const string ownerSql = """
            SELECT s.user_id AS UserId,
                   s.organization_id AS OrganizationId,
                   COALESCE(u.subscription_plan_id, o.subscription_plan_id, 1) AS PlanId
            FROM short_urls s
            LEFT JOIN users u ON u.id = s.user_id
            LEFT JOIN organizations o ON o.id = s.organization_id
            WHERE s.id = @ShortUrlId
            """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var owner = await connection.QuerySingleOrDefaultAsync<OwnerRow>(
            new CommandDefinition(ownerSql, new { ShortUrlId = shortUrlId }, cancellationToken: cancellationToken));

        if (owner is null) return null;

        var plan = (enSubscriptionPlan)(byte)owner.PlanId;
        if (userContext.IsAdmin) return new AccessibleOwner(plan);
        if (owner.UserId is { } ownerId) return ownerId == userContext.UserId ? new AccessibleOwner(plan) : null;
        if (owner.OrganizationId is not { } organizationId) return null;

        return (await organizationAuthorization.RequireMembershipAsync(organizationId, cancellationToken)).IsSuccess
            ? new AccessibleOwner(plan)
            : null;
    }

    private async Task<enSubscriptionPlan> QueryPlanAsync(string table, Guid id, CancellationToken cancellationToken)
    {
        var sql = $"SELECT subscription_plan_id FROM {table} WHERE id = @Id";

        using var connection = sqlConnectionFactory.CreateConnection();
        var plan = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return plan is { } value ? (enSubscriptionPlan)(byte)value : enSubscriptionPlan.Free;
    }

    private sealed class OwnerRow
    {
        public Guid? UserId { get; init; }
        public Guid? OrganizationId { get; init; }
        public int PlanId { get; init; }
    }
}

/// <summary>Utility for enforcing read-side click history retention limits based on subscription plans.</summary>
internal static class AnalyticsRetention
{
    public static DateTime? ClampFrom(DateTime? requestedFrom, int retentionDays, DateTime utcNow)
    {
        if (retentionDays < 0) return requestedFrom;

        var earliest = retentionDays == 0 ? utcNow : utcNow.AddDays(-retentionDays);
        return requestedFrom is { } from && from > earliest ? from : earliest;
    }
}
