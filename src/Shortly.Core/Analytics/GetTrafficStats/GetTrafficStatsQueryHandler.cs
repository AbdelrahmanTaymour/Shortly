using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Analytics.GetDeviceStats;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetTrafficStats;

internal sealed class GetTrafficStatsQueryHandler(ISqlConnectionFactory sqlConnectionFactory,
    AnalyticsScopeResolver scopeResolver)
    : IQueryHandler<GetTrafficStatsQuery, TrafficStatsResponse>
{
    private const int TopN = 10;

    public async Task<Result<TrafficStatsResponse>> Handle(GetTrafficStatsQuery request, CancellationToken cancellationToken)
    {
        var scopeResult = await scopeResolver.ResolveAsync(
            request.ShortUrlId, request.From, request.To, AnalyticsFeature.LinkAnalysis, cancellationToken);
        if (scopeResult.IsFailure) return Result.Failure<TrafficStatsResponse>(scopeResult.Error);
        var scope = scopeResult.Value;

        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
            WITH scoped AS (
                SELECT
                    NULLIF(BTRIM(ce.traffic_source), '') AS traffic_source
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            ),
            by_source AS (
                SELECT
                    traffic_source AS name,
                    COUNT(*) AS clicks,
                    ROW_NUMBER() OVER (
                        ORDER BY COUNT(*) DESC, traffic_source
                    ) AS rn
                FROM scoped
                WHERE traffic_source IS NOT NULL
                GROUP BY traffic_source
            )
            SELECT
                name AS "Name",
                clicks AS "Clicks"
            FROM by_source
            WHERE rn <= @TopN;

            WITH scoped AS (
                SELECT
                    NULLIF(BTRIM(ce.referrer_domain), '') AS referrer_domain
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            ),
            by_referrer AS (
                SELECT
                    referrer_domain AS name,
                    COUNT(*) AS clicks,
                    ROW_NUMBER() OVER (
                        ORDER BY COUNT(*) DESC, referrer_domain
                    ) AS rn
                FROM scoped
                WHERE referrer_domain IS NOT NULL
                GROUP BY referrer_domain
            )
            SELECT
                name AS "Domain",
                clicks AS "Clicks"
            FROM by_referrer
            WHERE rn <= @TopN;

            WITH scoped AS (
                SELECT
                    NULLIF(BTRIM(ce.utm_campaign), '') AS utm_campaign,
                    ce.utm_source,
                    ce.utm_medium
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            ),
            by_campaign AS (
                SELECT
                    utm_campaign AS name,
                    utm_source,
                    utm_medium,
                    COUNT(*) AS clicks,
                    ROW_NUMBER() OVER (
                        ORDER BY
                            COUNT(*) DESC,
                            utm_campaign,
                            utm_source,
                            utm_medium
                    ) AS rn
                FROM scoped
                WHERE utm_campaign IS NOT NULL
                GROUP BY utm_campaign, utm_source, utm_medium
            )
            SELECT
                name AS "CampaignName",
                utm_source AS "Source",
                utm_medium AS "Medium",
                clicks AS "Clicks"
            FROM by_campaign
            WHERE rn <= @TopN;

            WITH scoped AS (
                SELECT
                    NULLIF(BTRIM(ce.traffic_source), '') AS traffic_source
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                COUNT(*) FILTER (
                    WHERE traffic_source = 'Direct'
                ) AS "Direct",
                COUNT(*) FILTER (
                    WHERE traffic_source = 'Search'
                ) AS "Search",
                COUNT(*) FILTER (
                    WHERE traffic_source = 'Social'
                ) AS "Social",
                COUNT(*) FILTER (
                    WHERE traffic_source = 'Referral'
                ) AS "Referral",
                COUNT(*) AS "Total"
            FROM scoped;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                scope.ShortUrlId,
                scope.UserId,
                scope.OrganizationId,
                scope.From,
                scope.To,
                TopN
            },
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var sourceRows = (await multi.ReadAsync<TrafficSourceRow>()).ToList();
        var referrerRows = (await multi.ReadAsync<ReferrerRow>()).ToList();
        var campaignRows = (await multi.ReadAsync<CampaignRow>()).ToList();
        var totals = await multi.ReadSingleAsync<TrafficTotalsRow>();

        return new TrafficStatsResponse
        {
            TopTrafficSources = sourceRows
                .Select(source => new DeviceBreakdown(
                    source.Name,
                    source.Clicks,
                    CalculatePercentage(source.Clicks, totals.Total)))
                .ToList(),

            TopReferrers = referrerRows
                .Select(referrer => new ReferrerBreakdown(
                    referrer.Domain,
                    referrer.Clicks,
                    CalculatePercentage(referrer.Clicks, totals.Total)))
                .ToList(),

            // UTM campaign breakdown needs CampaignTracking; the rest of the traffic report is unaffected.
            TopCampaigns = !scope.IncludeCampaigns ? [] : campaignRows
                .Select(campaign => new CampaignBreakdown(
                    campaign.CampaignName,
                    campaign.Source,
                    campaign.Medium,
                    campaign.Clicks,
                    CalculatePercentage(campaign.Clicks, totals.Total)))
                .ToList(),

            DirectPercentage = CalculatePercentage(totals.Direct, totals.Total),
            SearchPercentage = CalculatePercentage(totals.Search, totals.Total),
            SocialPercentage = CalculatePercentage(totals.Social, totals.Total),
            ReferralPercentage = CalculatePercentage(totals.Referral, totals.Total)
        };
    }

    private static double CalculatePercentage(long clicks, long total) =>
        total == 0 ? 0 : clicks * 100.0 / total;

    private sealed class TrafficSourceRow
    {
        public string Name { get; init; } = string.Empty;
        public int Clicks { get; init; }
    }

    private sealed class ReferrerRow
    {
        public string Domain { get; init; } = string.Empty;
        public int Clicks { get; init; }
    }

    private sealed class CampaignRow
    {
        public string CampaignName { get; init; } = string.Empty;
        public string? Source { get; init; }
        public string? Medium { get; init; }
        public int Clicks { get; init; }
    }

    private sealed class TrafficTotalsRow
    {
        public long Direct { get; init; }
        public long Search { get; init; }
        public long Social { get; init; }
        public long Referral { get; init; }
        public long Total { get; init; }
    }
}