using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetGeographicalStats;

internal sealed class GetGeographicalStatsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetGeographicalStatsQuery, GeographicalStatsResponse>
{
    private const int TopN = 5;

    public async Task<Result<GeographicalStatsResponse>> Handle(GetGeographicalStatsQuery request, CancellationToken cancellationToken)
    {
        var scope = new AnalyticsScope(request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To);
        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
            WITH scoped AS (
                SELECT ce.country, ce.city
                FROM click_events ce
                JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            ),
            total AS (SELECT COUNT(*)::float8 AS n FROM scoped),
            by_country AS (
                SELECT country AS name, COUNT(*) AS clicks,
                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn
                FROM scoped GROUP BY country
            ),
            by_city AS (
                SELECT city AS name, COUNT(*) AS clicks,
                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn
                FROM scoped GROUP BY city
            )
            SELECT 'country' AS dimension, name, clicks FROM by_country WHERE rn <= @TopN
            UNION ALL
            SELECT 'city' AS dimension, name, clicks FROM by_city WHERE rn <= @TopN;

            SELECT COUNT(DISTINCT country) AS countries, COUNT(DISTINCT city) AS cities FROM scoped;
            """;

        using var multi = await connection.QueryMultipleAsync(sql, Params(request, TopN));

        var rows = (await multi.ReadAsync<GeoRow>()).ToList();
        var totals = await multi.ReadSingleAsync<TotalsRow>();
        var totalClicks = Math.Max(rows.Sum(r => r.Clicks), 1);

        var countries = rows.Where(r => r.Dimension == "country")
            .Select(r => new GeoBreakdown(r.Name, r.Clicks, r.Clicks * 100.0 / totalClicks)).ToList();
        var cities = rows.Where(r => r.Dimension == "city")
            .Select(r => new GeoBreakdown(r.Name, r.Clicks, r.Clicks * 100.0 / totalClicks)).ToList();

        return new GeographicalStatsResponse
        {
            TopCountries = countries,
            TopCities = cities,
            TotalDistinctCountries = totals.Countries,
            TotalDistinctCities = totals.Cities
        };
    }

    private static object Params(GetGeographicalStatsQuery r, int topN) => new
    {
        r.ShortUrlId, r.UserId, r.OrganizationId, r.From, r.To, TopN = topN
    };

    private sealed class GeoRow 
    { 
        public string Dimension { get; init; } = ""; 
        public string Name { get; init; } = ""; 
        public int Clicks { get; init; } 
    }
    private sealed class TotalsRow { 
        public int Countries { get; init; } 
        public int Cities { get; init; } 
    }
}