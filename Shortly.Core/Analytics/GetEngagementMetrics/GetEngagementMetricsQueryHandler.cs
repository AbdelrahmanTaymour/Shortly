using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetEngagementMetrics;

internal sealed class GetEngagementMetricsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetEngagementMetricsQuery, EngagementMetricsResponse>
{
    public async Task<Result<EngagementMetricsResponse>> Handle(GetEngagementMetricsQuery request, CancellationToken cancellationToken)
    {
        var scope = new AnalyticsScope(request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To);
        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
            WITH scoped AS (
                SELECT ce.session_id, ce.clicked_at
                FROM click_events ce
                JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            ),
            sessions AS (
                SELECT session_id, COUNT(*) AS clicks,
                       MAX(clicked_at) - MIN(clicked_at) AS duration
                FROM scoped GROUP BY session_id
            )
            SELECT
                COUNT(*)                                                  AS SessionCount,
                COUNT(*) FILTER (WHERE clicks = 1)                        AS SingleClickSessions,
                COUNT(*) FILTER (WHERE clicks > 1)                        AS ReturningVisitors,
                COUNT(*) FILTER (WHERE clicks = 1)                        AS NewVisitors,
                COALESCE(AVG(clicks), 0)                                  AS ClicksPerSession,
                COALESCE(AVG(EXTRACT(EPOCH FROM duration)), 0)            AS AvgSessionSeconds
            FROM sessions
            """;

        var row = await connection.QuerySingleAsync<EngagementRow>(sql, new
        {
            request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To
        });

        var bounceRate = row.SessionCount == 0 ? 0 : row.SingleClickSessions * 100.0 / row.SessionCount;
        var returnRate = row.SessionCount == 0 ? 0 : row.ReturningVisitors * 100.0 / row.SessionCount;

        return new EngagementMetricsResponse
        {
            BounceRate = bounceRate,
            ReturnVisitorRate = returnRate,
            NewVisitors = row.NewVisitors,
            ReturningVisitors = row.ReturningVisitors,
            AverageSessionDuration = TimeSpan.FromSeconds(row.AvgSessionSeconds),
            ClicksPerSession = row.ClicksPerSession
        };
    }

    private sealed class EngagementRow
    {
        public int SessionCount { get; init; }
        public int SingleClickSessions { get; init; }
        public int ReturningVisitors { get; init; }
        public int NewVisitors { get; init; }
        public double ClicksPerSession { get; init; }
        public double AvgSessionSeconds { get; init; }
    }
}