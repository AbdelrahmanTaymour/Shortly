using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetAnalyticsOverview;

internal sealed class GetAnalyticsOverviewQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetAnalyticsOverviewQuery, AnalyticsOverviewResponse>
{
    public async Task<Result<AnalyticsOverviewResponse>> Handle(GetAnalyticsOverviewQuery request, CancellationToken cancellationToken)
    {
        var scope = new AnalyticsScope(request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To);
        
        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
            WITH scoped AS (
                SELECT ce.clicked_at, ce.session_id, ce.country, ce.referrer_domain
                FROM click_events ce
                JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                COUNT(*)                                                              AS TotalClicks,
                COUNT(DISTINCT session_id)                                            AS UniqueClicks,
                COUNT(DISTINCT clicked_at::date)                                      AS ActiveDays,
                MIN(clicked_at)                                                       AS FirstClickAtUtc,
                MAX(clicked_at)                                                       AS LastClickAtUtc,
                COUNT(*) FILTER (WHERE clicked_at::date = CURRENT_DATE)               AS ClicksToday,
                COUNT(*) FILTER (WHERE clicked_at >= date_trunc('week', now()))       AS ClicksThisWeek,
                COUNT(*) FILTER (WHERE clicked_at >= date_trunc('month', now()))      AS ClicksThisMonth,
                (SELECT country FROM scoped WHERE country IS NOT NULL GROUP BY country ORDER BY COUNT(*) DESC LIMIT 1)         AS TopCountry,
                (SELECT referrer_domain FROM scoped WHERE referrer_domain IS NOT NULL GROUP BY referrer_domain ORDER BY COUNT(*) DESC LIMIT 1) AS TopReferrer
            FROM scoped
            """;

        var row = await connection.QuerySingleAsync<OverviewRow>(sql, BuildParameters(request));

        var activeDays = Math.Max(row.ActiveDays, 1);
        var averagePerDay = row.TotalClicks / (double)activeDays;

        return new AnalyticsOverviewResponse
        {
            TotalClicks = row.TotalClicks,
            UniqueClicks = row.UniqueClicks,
            ActiveDays = row.ActiveDays,
            AverageClicksPerDay = averagePerDay,
            FirstClickAtUtc = row.FirstClickAtUtc,
            LastClickAtUtc = row.LastClickAtUtc,
            ClicksToday = row.ClicksToday,
            ClicksThisWeek = row.ClicksThisWeek,
            ClicksThisMonth = row.ClicksThisMonth,
            TopCountry = row.TopCountry,
            TopReferrer = row.TopReferrer
        };
    }

    private static object BuildParameters(GetAnalyticsOverviewQuery r) => new
    {
        r.ShortUrlId, r.UserId, r.OrganizationId, r.From, r.To
    };

    private sealed class OverviewRow
    {
        public long TotalClicks { get; init; }
        public long UniqueClicks { get; init; }
        public int ActiveDays { get; init; }
        public DateTime? FirstClickAtUtc { get; init; }
        public DateTime? LastClickAtUtc { get; init; }
        public long ClicksToday { get; init; }
        public long ClicksThisWeek { get; init; }
        public long ClicksThisMonth { get; init; }
        public string? TopCountry { get; init; }
        public string? TopReferrer { get; init; }
    }
}