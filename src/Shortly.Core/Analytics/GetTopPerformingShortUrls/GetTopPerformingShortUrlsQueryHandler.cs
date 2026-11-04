using Dapper;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetTopPerformingShortUrls;


internal sealed class GetTopPerformingShortUrlsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    AnalyticsScopeResolver scopeResolver,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetTopPerformingShortUrlsQuery, IReadOnlyList<TopPerformingUrlResponse>>
{
    public async Task<Result<IReadOnlyList<TopPerformingUrlResponse>>> Handle(
        GetTopPerformingShortUrlsQuery request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await scopeResolver.ResolveAsync(null, null, null, AnalyticsFeature.LinkAnalysis, cancellationToken);
        if (scopeResult.IsFailure) return Result.Failure<IReadOnlyList<TopPerformingUrlResponse>>(scopeResult.Error);
        var scope = scopeResult.Value;

        var topCount = Math.Clamp(request.TopCount, 1, 100);

        DateTime? createdAfter = request.Timeframe is null
            ? null
            : dateTimeProvider.UtcNow.Subtract(request.Timeframe.Value);

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                s.id AS "ShortUrlId",
                s.short_code AS "ShortCode",
                s.original_url AS "OriginalUrl",
                s.total_clicks AS "TotalClicks",
                s.created_at_utc AS "CreatedAtUtc",
                (
                    SELECT MAX(ce.clicked_at_utc)
                    FROM click_events ce
                    WHERE ce.short_url_id = s.id
                      AND (@From::timestamptz IS NULL OR ce.clicked_at_utc >= @From::timestamptz)
                ) AS "LastClickAtUtc",
                CASE WHEN @IncludeGeo THEN (
                    SELECT ce.country
                    FROM click_events ce
                    WHERE ce.short_url_id = s.id
                      AND NULLIF(BTRIM(ce.country), '') IS NOT NULL
                      AND (@From::timestamptz IS NULL OR ce.clicked_at_utc >= @From::timestamptz)
                    GROUP BY ce.country
                    ORDER BY COUNT(*) DESC, ce.country
                    LIMIT 1
                ) END AS "TopCountry"
            FROM short_urls s
            WHERE s.is_active
              AND (@UserId IS NULL OR s.user_id = @UserId)
              AND (@OrganizationId IS NULL OR s.organization_id = @OrganizationId)
              AND (
                  @CreatedAfter::timestamptz IS NULL
                  OR s.created_at_utc >= @CreatedAfter::timestamptz
              )
            ORDER BY
                s.total_clicks DESC,
                s.created_at_utc DESC,
                s.id
            LIMIT @TopCount;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                scope.UserId,
                scope.OrganizationId,
                CreatedAfter = createdAfter,
                scope.From,
                IncludeGeo = scope.IncludeGeo,
                TopCount = topCount
            },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TopPerformingUrlResponse>(command);

        return rows.ToList();
    }
}