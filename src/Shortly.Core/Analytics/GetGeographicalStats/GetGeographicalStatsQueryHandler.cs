using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetGeographicalStats;

internal sealed class GetGeographicalStatsQueryHandler(ISqlConnectionFactory sqlConnectionFactory,
    AnalyticsScopeResolver scopeResolver)
    : IQueryHandler<GetGeographicalStatsQuery, GeographicalStatsResponse>
{
    private const int TopN = 5;

    public async Task<Result<GeographicalStatsResponse>> Handle(
        GetGeographicalStatsQuery request,
        CancellationToken cancellationToken)
    {
        var scopeResult = await scopeResolver.ResolveAsync(
            request.ShortUrlId, request.From, request.To, AnalyticsFeature.GeoDeviceAnalytics, cancellationToken);
        if (scopeResult.IsFailure) return Result.Failure<GeographicalStatsResponse>(scopeResult.Error);
        var scope = scopeResult.Value;

        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
            WITH scoped AS (
                SELECT
                    NULLIF(BTRIM(ce.country), '') AS country,
                    NULLIF(BTRIM(ce.city), '') AS city
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            ),
            by_country AS (
                SELECT
                    country AS name,
                    COUNT(*) AS clicks,
                    ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, country) AS rn
                FROM scoped
                WHERE country IS NOT NULL
                GROUP BY country
            ),
            by_city AS (
                SELECT
                    city AS name,
                    COUNT(*) AS clicks,
                    ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC, city) AS rn
                FROM scoped
                WHERE city IS NOT NULL
                GROUP BY city
            )
            SELECT 'country' AS dimension, name, clicks
            FROM by_country
            WHERE rn <= @TopN

            UNION ALL

            SELECT 'city' AS dimension, name, clicks
            FROM by_city
            WHERE rn <= @TopN;

            WITH scoped AS (
                SELECT
                    NULLIF(BTRIM(ce.country), '') AS country,
                    NULLIF(BTRIM(ce.city), '') AS city
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                COUNT(DISTINCT country) AS countries,
                COUNT(DISTINCT city) AS cities,
                COUNT(*) AS total_clicks
            FROM scoped;
            """;

        var command = new CommandDefinition(
            sql,
            Params(scope, TopN),
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var rows = (await multi.ReadAsync<GeoRow>()).ToList();
        var totals = await multi.ReadSingleAsync<TotalsRow>();

        var countries = rows
            .Where(r => r.Dimension == "country")
            .Select(r => new GeoBreakdown(
                r.Name,
                r.Clicks,
                totals.TotalClicks == 0 ? 0 : r.Clicks * 100.0 / totals.TotalClicks))
            .ToList();

        var cities = rows
            .Where(r => r.Dimension == "city")
            .Select(r => new GeoBreakdown(
                r.Name,
                r.Clicks,
                totals.TotalClicks == 0 ? 0 : r.Clicks * 100.0 / totals.TotalClicks))
            .ToList();

        return new GeographicalStatsResponse
        {
            TopCountries = countries,
            TopCities = cities,
            TotalDistinctCountries = totals.Countries,
            TotalDistinctCities = totals.Cities
        };
    }

    private static object Params(AnalyticsScope r, int topN) => new
    {
        r.ShortUrlId,
        r.UserId,
        r.OrganizationId,
        r.From,
        r.To,
        TopN = topN
    };

    private sealed class GeoRow
    {
        public string Dimension { get; init; } = "";
        public string Name { get; init; } = "";
        public int Clicks { get; init; }
    }

    private sealed class TotalsRow
    {
        public int Countries { get; init; }
        public int Cities { get; init; }
        public long TotalClicks { get; init; }
    }
}