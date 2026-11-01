using Dapper;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Analytics.GetTimeSeriesStats;

internal sealed class GetTimeSeriesStatsQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetTimeSeriesStatsQuery, TimeSeriesStatsResponse>
{
    public async Task<Result<TimeSeriesStatsResponse>> Handle(
        GetTimeSeriesStatsQuery request,
        CancellationToken cancellationToken)
    {
        var scope = new AnalyticsScope(
            request.ShortUrlId,
            request.UserId,
            request.OrganizationId,
            request.From,
            request.To);

        using var connection = sqlConnectionFactory.CreateConnection();

        var sql = $"""
            WITH scoped AS (
                SELECT
                    ce.clicked_at_utc,
                    ce.session_id
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                (clicked_at_utc AT TIME ZONE 'UTC')::date AS "BucketStart",
                COUNT(*) AS "Clicks",
                COUNT(DISTINCT session_id) AS "UniqueClicks"
            FROM scoped
            GROUP BY (clicked_at_utc AT TIME ZONE 'UTC')::date
            ORDER BY "BucketStart";

            WITH scoped AS (
                SELECT ce.clicked_at_utc
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                EXTRACT(HOUR FROM clicked_at_utc AT TIME ZONE 'UTC')::int AS "Hour",
                COUNT(*) AS "Clicks"
            FROM scoped
            GROUP BY "Hour"
            ORDER BY "Hour";

            WITH scoped AS (
                SELECT ce.clicked_at_utc
                FROM click_events ce
                INNER JOIN short_urls s ON s.id = ce.short_url_id
                WHERE {scope.BuildWhereClause()}
            )
            SELECT
                EXTRACT(DOW FROM clicked_at_utc AT TIME ZONE 'UTC')::int AS "Day",
                COUNT(*) AS "Clicks"
            FROM scoped
            GROUP BY "Day"
            ORDER BY "Day";
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                request.ShortUrlId,
                request.UserId,
                request.OrganizationId,
                request.From,
                request.To
            },
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var trend = (await multi.ReadAsync<TimeSeriesPoint>()).ToList();

        IReadOnlyDictionary<int, long> byHour =
            (await multi.ReadAsync<HourRow>())
            .ToDictionary(x => x.Hour, x => x.Clicks);

        IReadOnlyDictionary<DayOfWeek, long> byDay =
            (await multi.ReadAsync<DayRow>())
            .ToDictionary(x => (DayOfWeek)x.Day, x => x.Clicks);

        var peakHour = byHour.Count == 0
            ? (int?)null
            : byHour.MaxBy(x => x.Value).Key;

        var peakDay = byDay.Count == 0
            ? (DayOfWeek?)null
            : byDay.MaxBy(x => x.Value).Key;

        var averagePerDay = trend.Count == 0
            ? 0
            : trend.Average(x => x.Clicks);

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

    private sealed class HourRow
    {
        public int Hour { get; init; }
        public long Clicks { get; init; }
    }

    private sealed class DayRow
    {
        public int Day { get; init; }
        public long Clicks { get; init; }
    }
}