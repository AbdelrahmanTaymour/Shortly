using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Analytics.GetDeviceStats;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetTrafficStats;

internal sealed class GetTrafficStatsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTrafficStatsQuery, TrafficStatsResponse>
{
    private const int TopN = 10;

    public async Task<Result<TrafficStatsResponse>> Handle(GetTrafficStatsQuery request,
        CancellationToken cancellationToken)
    {
        var scope = new AnalyticsScope(request.ShortUrlId, request.UserId, request.OrganizationId, request.From,
            request.To);
        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
                   WITH scoped AS (
                       SELECT ce.traffic_source, ce.referrer_domain, ce.utm_campaign, ce.utm_source, ce.utm_medium
                       FROM click_events ce
                       JOIN short_urls s ON s.id = ce.short_url_id
                       WHERE {scope.BuildWhereClause()}
                   ),
                   by_source AS (SELECT traffic_source AS name, COUNT(*) AS clicks, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn FROM scoped GROUP BY traffic_source),
                   by_referrer AS (SELECT referrer_domain AS name, COUNT(*) AS clicks, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn FROM scoped WHERE referrer_domain IS NOT NULL GROUP BY referrer_domain),
                   by_campaign AS (SELECT utm_campaign AS name, utm_source, utm_medium, COUNT(*) AS clicks, ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn FROM scoped WHERE utm_campaign IS NOT NULL GROUP BY utm_campaign, utm_source, utm_medium)
                   SELECT name, clicks FROM by_source WHERE rn <= @TopN6;

                   SELECT name AS Domain, clicks AS Clicks FROM by_referrer WHERE rn <= @TopN;
                   SELECT name AS CampaignName, utm_source AS Source, utm_medium AS Medium, clicks AS Clicks FROM by_campaign WHERE rn <= @TopN;

                   SELECT
                       COUNT(*) FILTER (WHERE traffic_source = 'Direct')   AS Direct,
                       COUNT(*) FILTER (WHERE traffic_source = 'Search')   AS Search,
                       COUNT(*) FILTER (WHERE traffic_source = 'Social')   AS Social,
                       COUNT(*) FILTER (WHERE traffic_source = 'Referral') AS Referral,
                       COUNT(*)                                            AS Total
                   FROM scoped
                   """;

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To, TopN, TopN6 = 6
        });

        var sources = (await multi.ReadAsync<DeviceBreakdown>()).ToList();
        var sourceTotal = Math.Max(sources.Sum(s => s.Clicks), 1);
        sources = sources.Select(s => s with { Percentage = s.Clicks * 100.0 / sourceTotal }).ToList();

        var referrers = (await multi.ReadAsync<ReferrerBreakdown>()).ToList();
        var referrerTotal = Math.Max(referrers.Sum(r => r.Clicks), 1);
        referrers = referrers.Select(r => r with { Percentage = r.Clicks * 100.0 / referrerTotal }).ToList();

        var campaigns = (await multi.ReadAsync<CampaignBreakdown>()).ToList();
        var campaignTotal = Math.Max(campaigns.Sum(c => c.Clicks), 1);
        campaigns = campaigns.Select(c => c with { Percentage = c.Clicks * 100.0 / campaignTotal }).ToList();

        var totals = await multi.ReadSingleAsync<TrafficTotalsRow>();
        var total = Math.Max(totals.Total, 1);


        return new TrafficStatsResponse
        {
            TopTrafficSources = sources,
            TopReferrers = referrers,
            TopCampaigns = campaigns,
            DirectPercentage = totals.Direct * 100.0 / total,
            SearchPercentage = totals.Search * 100.0 / total,
            SocialPercentage = totals.Social * 100.0 / total,
            ReferralPercentage = totals.Referral * 100.0 / total
        };
    }

    private sealed class TrafficTotalsRow
    {
        public int Direct { get; init; }
        public int Search { get; init; }
        public int Social { get; init; }
        public int Referral { get; init; }
        public int Total { get; init; }
    }
}