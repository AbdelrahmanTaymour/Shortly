using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetTimeSeriesStats;

internal sealed class GetTimeSeriesStatsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTimeSeriesStatsQuery, TimeSeriesStatsResponse>
{
    public async Task<Result<TimeSeriesStatsResponse>> Handle(GetTimeSeriesStatsQuery request, CancellationToken cancellationToken)
    {
        var scope = new AnalyticsScope(request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To);
        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
            WITH scoped AS (
                SELECT ce.clicked_at, ce.session_id
                FROM click_events ce
                JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT clicked_at::date AS BucketStart, COUNT(*) AS Clicks, COUNT(DISTINCT session_id) AS UniqueClicks
            FROM scoped GROUP BY clicked_at::date ORDER BY BucketStart;

            SELECT EXTRACT(HOUR FROM clicked_at)::int AS Hour, COUNT(*) AS Clicks FROM scoped GROUP BY Hour;
            SELECT EXTRACT(DOW FROM clicked_at)::int AS Day, COUNT(*) AS Clicks FROM scoped GROUP BY Day;
            """;

        using var multi = await connection.QueryMultipleAsync(sql, new { request.ShortUrlId, request.UserId, request.OrganizationId, request.From, request.To });

        var trend = (await multi.ReadAsync<TimeSeriesPoint>()).ToList();
        var byHour = (await multi.ReadAsync<(int Hour, int Clicks)>()).ToDictionary(x => x.Hour, x => x.Clicks);
        var byDay = (await multi.ReadAsync<(int Day, int Clicks)>())
            .ToDictionary(x => (DayOfWeek)x.Day, x => x.Clicks);

        var peakHour = byHour.Count == 0 ? 0 : byHour.MaxBy(kv => kv.Value).Key;
        var peakDay = byDay.Count == 0 ? DayOfWeek.Sunday : byDay.MaxBy(kv => kv.Value).Key;
        var averagePerDay = trend.Count == 0 ? 0 : trend.Average(p => p.Clicks);

        return new TimeSeriesStatsResponse
        {
            BucketType = "daily",
            Trend = trend,
            ClicksByHourOfDay = byHour,
            ClicksByDayOfWeek = byDay,
            PeakHour = peakHour,
            PeakDay = peakDay,
            AverageClicksPerDay = averagePerDay
        };
    }
}