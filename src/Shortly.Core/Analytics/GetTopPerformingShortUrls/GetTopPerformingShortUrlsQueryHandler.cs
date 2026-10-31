using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetTopPerformingShortUrls;

internal sealed class GetTopPerformingShortUrlsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTopPerformingShortUrlsQuery, IReadOnlyList<TopPerformingUrlResponse>>
{
    public async Task<Result<IReadOnlyList<TopPerformingUrlResponse>>> Handle(GetTopPerformingShortUrlsQuery request, CancellationToken cancellationToken)
    {
        var topCount = Math.Clamp(request.TopCount, 1, 100);
        var createdAfter = request.Timeframe is null ? (DateTime?)null : DateTime.UtcNow.Subtract(request.Timeframe.Value);

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT s.id AS ShortUrlId, s.short_code AS ShortCode, s.original_url AS OriginalUrl,
                                  s.total_clicks AS TotalClicks, s.created_at_utc AS CreatedAtUtc,
                                  (SELECT MAX(ce.clicked_at) FROM click_events ce WHERE ce.short_url_id = s.id) AS LastClickAtUtc,
                                  (SELECT ce.country FROM click_events ce WHERE ce.short_url_id = s.id
                                   GROUP BY ce.country ORDER BY COUNT(*) DESC LIMIT 1) AS TopCountry
                           FROM short_urls s
                           WHERE s.is_active
                             AND (@UserId IS NULL OR s.user_id = @UserId)
                             AND (@OrganizationId IS NULL OR s.organization_id = @OrganizationId)
                             AND (@CreatedAfter IS NULL OR s.created_at_utc >= @CreatedAfter)
                           ORDER BY s.total_clicks DESC, s.created_at_utc DESC
                           LIMIT @TopCount
                           """;

        var rows = await connection.QueryAsync<TopPerformingUrlResponse>(sql, new
        {
            request.UserId, request.OrganizationId, CreatedAfter = createdAfter, TopCount = topCount
        });

        return rows.ToList();
    }
}