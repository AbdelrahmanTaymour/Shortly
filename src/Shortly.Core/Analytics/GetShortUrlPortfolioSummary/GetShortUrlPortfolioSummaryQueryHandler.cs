using Dapper;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetShortUrlPortfolioSummary;

internal sealed class GetShortUrlPortfolioSummaryQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    AnalyticsScopeResolver scopeResolver,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetShortUrlPortfolioSummaryQuery, ShortUrlPortfolioSummaryResponse>
{
    public async Task<Result<ShortUrlPortfolioSummaryResponse>> Handle(
        GetShortUrlPortfolioSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await scopeResolver.ResolveAsync(null, null, null, AnalyticsFeature.None, cancellationToken);
        if (scopeResult.IsFailure) return Result.Failure<ShortUrlPortfolioSummaryResponse>(scopeResult.Error);
        var scope = scopeResult.Value;

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               COUNT(*) AS TotalUrls,
                               COUNT(*) FILTER (WHERE is_active) AS ActiveUrls,
                               COUNT(*) FILTER (
                                   WHERE expires_at IS NOT NULL AND expires_at <= @UtcNow
                               ) AS ExpiredUrls,
                               COUNT(*) FILTER (WHERE is_private) AS PrivateUrls,
                               COUNT(*) FILTER (WHERE is_password_protected) AS PasswordProtectedUrls,
                               COALESCE(SUM(total_clicks), 0) AS TotalClicks,
                               COALESCE(
                                   SUM(total_clicks)::float8 / NULLIF(COUNT(*), 0),
                                   0.0
                               ) AS AverageClicksPerUrl
                           FROM short_urls
                           WHERE (@UserId IS NULL OR user_id = @UserId)
                             AND (@OrganizationId IS NULL OR organization_id = @OrganizationId);
                           """;

        var command = new CommandDefinition(
            sql,
            new { scope.UserId, scope.OrganizationId, UtcNow = dateTimeProvider.UtcNow },
            cancellationToken: cancellationToken);

        var response =
            await connection.QuerySingleAsync<ShortUrlPortfolioSummaryResponse>(command);

        return response;
    }
}