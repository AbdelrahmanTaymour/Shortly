using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetEngagementMetrics;

internal sealed class GetEngagementMetricsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    AnalyticsScopeResolver scopeResolver)
    : IQueryHandler<GetEngagementMetricsQuery, EngagementMetricsResponse>
{
    public async Task<Result<EngagementMetricsResponse>> Handle(
        GetEngagementMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await scopeResolver.ResolveAsync(
            request.ShortUrlId, request.From, request.To, AnalyticsFeature.LinkAnalysis, cancellationToken);
        if (scopeResult.IsFailure) return Result.Failure<EngagementMetricsResponse>(scopeResult.Error);
        var scope = scopeResult.Value;

        var sql = $"""
            WITH scoped AS (
                SELECT
                    ce.session_id AS SessionId,
                    ce.clicked_at_utc AS ClickedAtUtc
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
                  AND NULLIF(BTRIM(ce.session_id), '') IS NOT NULL
            ),
            sessions AS (
                SELECT
                    SessionId,
                    COUNT(*) AS Clicks,
                    MAX(ClickedAtUtc) - MIN(ClickedAtUtc) AS Duration
                FROM scoped
                GROUP BY SessionId
            )
            SELECT
                COUNT(*) AS SessionCount,
                COUNT(*) FILTER (WHERE Clicks = 1) AS SingleClickSessions,
                COUNT(*) FILTER (WHERE Clicks > 1) AS MultiClickSessions,
                COALESCE(AVG(Clicks), 0) AS ClicksPerSession,
                COALESCE(AVG(EXTRACT(EPOCH FROM Duration)), 0) AS AvgSessionSeconds
            FROM sessions;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                scope.ShortUrlId,
                scope.UserId,
                scope.OrganizationId,
                scope.From,
                scope.To
            },
            cancellationToken: cancellationToken);

        using var connection = sqlConnectionFactory.CreateConnection();

        var row = await connection.QuerySingleAsync<EngagementRow>(command);

        var bounceRate = row.SessionCount == 0
            ? 0
            : row.SingleClickSessions * 100.0 / row.SessionCount;

        var multiClickSessionRate = row.SessionCount == 0
            ? 0
            : row.MultiClickSessions * 100.0 / row.SessionCount;

        return new EngagementMetricsResponse
        {
            BounceRate = bounceRate,
            ReturnVisitorRate = multiClickSessionRate,
            NewVisitors = row.SingleClickSessions,
            ReturningVisitors = row.MultiClickSessions,
            AverageSessionDuration = TimeSpan.FromSeconds(row.AvgSessionSeconds),
            ClicksPerSession = row.ClicksPerSession
        };
    }

    private sealed class EngagementRow
    {
        public long SessionCount { get; init; }
        public int SingleClickSessions { get; init; }
        public int MultiClickSessions { get; init; }
        public double ClicksPerSession { get; init; }
        public double AvgSessionSeconds { get; init; }
    }
}