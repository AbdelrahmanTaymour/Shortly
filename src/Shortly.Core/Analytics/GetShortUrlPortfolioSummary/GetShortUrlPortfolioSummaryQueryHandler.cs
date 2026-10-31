using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetShortUrlPortfolioSummary;

internal sealed class GetShortUrlPortfolioSummaryQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetShortUrlPortfolioSummaryQuery, ShortUrlPortfolioSummaryResponse>
{
    public async Task<Result<ShortUrlPortfolioSummaryResponse>> Handle(
        GetShortUrlPortfolioSummaryQuery request, 
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               COUNT(*)                                                           AS TotalUrls,
                               COUNT(*) FILTER (WHERE is_active)                                  AS ActiveUrls,
                               COUNT(*) FILTER (WHERE expires_at IS NOT NULL AND expires_at <= now()) AS ExpiredUrls,
                               COUNT(*) FILTER (WHERE is_private)                                 AS PrivateUrls,
                               COUNT(*) FILTER (WHERE is_password_protected)                      AS PasswordProtectedUrls,
                               COALESCE(SUM(total_clicks), 0)                                     AS TotalClicks,
                               CASE WHEN COUNT(*) = 0 THEN 0.0 ELSE COALESCE(SUM(total_clicks), 0)::float8 / COUNT(*) END AS AverageClicksPerUrl
                           FROM short_urls
                           WHERE (@UserId IS NULL OR user_id = @UserId)
                             AND (@OrganizationId IS NULL OR organization_id = @OrganizationId)
                           """;

        var response = await connection.QueryFirstOrDefaultAsync<ShortUrlPortfolioSummaryResponse>(
            sql, new { request.UserId, request.OrganizationId });

        return response;
    }
}