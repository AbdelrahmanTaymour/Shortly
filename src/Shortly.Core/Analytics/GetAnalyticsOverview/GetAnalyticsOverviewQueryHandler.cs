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
                SELECT
                    ce.clicked_at_utc AS ClickedAtUtc,
                    ce.session_id AS SessionId,
                    ce.country AS Country,
                    ce.referrer_domain AS ReferrerDomain
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                COUNT(*) AS TotalClicks,
                COUNT(DISTINCT SessionId) AS UniqueClicks,
                COUNT(DISTINCT ClickedAtUtc::date) AS ActiveDays,
                MIN(ClickedAtUtc) AS FirstClickAtUtc,
                MAX(ClickedAtUtc) AS LastClickAtUtc,
            
                COUNT(*) FILTER (
                    WHERE ClickedAtUtc::date = CURRENT_DATE
                ) AS ClicksToday,
            
                COUNT(*) FILTER (
                    WHERE ClickedAtUtc >= date_trunc('week', NOW())
                ) AS ClicksThisWeek,
            
                COUNT(*) FILTER (
                    WHERE ClickedAtUtc >= date_trunc('month', NOW())
                ) AS ClicksThisMonth,
            
                (
                    SELECT Country
                    FROM scoped
                    WHERE NULLIF(TRIM(Country), '') IS NOT NULL
                    GROUP BY Country
                    ORDER BY COUNT(*) DESC, Country
                    LIMIT 1
                ) AS TopCountry,
            
                (
                    SELECT ReferrerDomain
                    FROM scoped
                    WHERE NULLIF(TRIM(ReferrerDomain), '') IS NOT NULL
                    GROUP BY ReferrerDomain
                    ORDER BY COUNT(*) DESC, ReferrerDomain
                    LIMIT 1
                ) AS TopReferrer
            
            FROM scoped;
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