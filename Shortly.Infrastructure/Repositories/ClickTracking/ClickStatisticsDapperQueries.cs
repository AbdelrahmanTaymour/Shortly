using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Logging;
using Shortly.Core.Abstractions.Data;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Analytics.DTOs;

namespace Shortly.Infrastructure.Repositories.ClickTracking;

/// <summary>
///     Dapper query implementation for analytics.
/// </summary>
public class ClickStatisticsDapperQueries(
    ISqlConnectionFactory sqlConnectionFactory,
    ILogger<ClickStatisticsDapperQueries> logger) : IClickStatisticsDapperQueries
{
    private enum DateBucket
    {
        Daily,
        Weekly
    }

    private static DateBucket ComputeBucket(DateTime? start, DateTime? end)
    {
        var s = start ?? DateTime.UtcNow.AddDays(-30);
        var e = end ?? DateTime.UtcNow;
        return (e - s).TotalDays > 90 ? DateBucket.Weekly : DateBucket.Daily;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PER-URL STATISTICS  (unchanged from previous revision)
    // ═════════════════════════════════════════════════════════════════════════

#region Per-URL Statistics

    public async Task<UrlOverview> GetUrlOverviewAsync(
        long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           -- RS1: Core click metrics (date-range scoped).
                           SELECT COUNT(*)                          AS TotalClicks,
                                  COUNT(DISTINCT ce.session_id)     AS UniqueClicks,
                                  COUNT(DISTINCT ce.clicked_at::date) AS ActiveDays,
                                  MIN(ce.clicked_at)                AS FirstClickDate,
                                  MAX(ce.clicked_at)                AS LastClickDate
                           FROM click_events ce
                           CROSS JOIN (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ) p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date;

                           -- RS2: Time-window counters.
                           SELECT COALESCE(SUM(CASE WHEN ce.clicked_at::date = CURRENT_TIMESTAMP::date THEN 1 ELSE 0 END), 0)::bigint AS ClicksToday,
                                  COALESCE(SUM(CASE WHEN ce.clicked_at >= (CURRENT_TIMESTAMP::date - INTERVAL '6 days') THEN 1 ELSE 0 END), 0)::bigint AS ClicksThisWeek,
                                  COALESCE(SUM(CASE WHEN ce.clicked_at >= date_trunc('month', CURRENT_TIMESTAMP) THEN 1 ELSE 0 END), 0)::bigint AS ClicksThisMonth
                           FROM click_events ce
                           WHERE ce.short_url_id = @Id AND ce.clicked_at >= date_trunc('month', CURRENT_TIMESTAMP);

                           -- RS3: Top country
                           SELECT COALESCE(NULLIF(ce.country, ''), 'Unknown') AS Name
                           FROM click_events ce
                           CROSS JOIN (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ) p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                             AND ce.country IS NOT NULL AND ce.country != ''
                           GROUP BY ce.country
                           ORDER BY COUNT(*) DESC
                           LIMIT 1;

                           -- RS4: Top referrer domain
                           SELECT COALESCE(NULLIF(ce.referrer_domain, ''), 'Direct') AS Name
                           FROM click_events ce
                           CROSS JOIN (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ) p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.referrer_domain
                           ORDER BY COUNT(*) DESC
                           LIMIT 1;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = shortUrlId, S = startDate, E = endDate },
                cancellationToken: ct));

        var (totalClicks, uniqueClicks, activeDays, firstClickDate, lastClickDate) =
            await multi.ReadSingleAsync<CoreMetricsRow>();
        var windowRow = await multi.ReadSingleAsync<TimeWindowRow>();
        var topCountry = await multi.ReadFirstOrDefaultAsync<string>();
        var topReferrer = await multi.ReadFirstOrDefaultAsync<string>();

        return new UrlOverview
        {
            Overview = new OverviewMetrics
            {
                TotalClicks = totalClicks,
                UniqueClicks = uniqueClicks,
                ActiveDays = activeDays,
                FirstClickDate = firstClickDate,
                LastClickDate = lastClickDate,
                ClicksToday = windowRow.ClicksToday,
                ClicksThisWeek = windowRow.ClicksThisWeek,
                ClicksThisMonth = windowRow.ClicksThisMonth,
                AverageClicksPerDay = activeDays > 0
                    ? Math.Round((double)totalClicks / activeDays, 2)
                    : 0,
                ClickThroughRate = totalClicks > 0
                    ? Math.Round((double)uniqueClicks / totalClicks, 4)
                    : 0
            },
            TopCountry = topCountry,
            TopReferrer = topReferrer
        };
    }

    public async Task<EngagementMetrics> GetUrlEngagementAsync(
        long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ),
                           SessionStats AS (
                               SELECT
                                   ce.session_id,
                                   COUNT(*) AS SessionClicks,
                                   EXTRACT(EPOCH FROM (MAX(ce.clicked_at) - MIN(ce.clicked_at)))::double precision AS DurationSeconds
                               FROM click_events ce
                               CROSS JOIN params p
                               WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                               GROUP BY ce.session_id
                           )
                           SELECT
                               COUNT(*)::bigint AS TotalSessions,
                               COALESCE(SUM(SessionClicks), 0)::bigint AS TotalClicksForEngagement,
                               COALESCE(SUM(CASE WHEN SessionClicks = 1 THEN 1 ELSE 0 END), 0)::bigint AS BouncedSessions,
                               COALESCE(SUM(CASE WHEN SessionClicks > 1 THEN 1 ELSE 0 END), 0)::bigint AS ReturningSessions,
                               COALESCE(AVG(CASE WHEN SessionClicks > 1 THEN DurationSeconds END), 0.0) AS AvgSessionDurationSeconds
                           FROM SessionStats;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var engRow = await connection.QuerySingleAsync<EngagementRow>(
            new CommandDefinition(sql, new { Id = shortUrlId, S = startDate, E = endDate },
                cancellationToken: ct));

        var totalSessions = engRow.TotalSessions;

        return new EngagementMetrics
        {
            BounceRate = totalSessions > 0
                ? Math.Round((double)engRow.BouncedSessions / totalSessions * 100, 2)
                : 0,
            ReturnVisitorRate = totalSessions > 0
                ? Math.Round((double)engRow.ReturningSessions / totalSessions * 100, 2)
                : 0,
            NewVisitors = engRow.BouncedSessions,
            ReturningVisitors = engRow.ReturningSessions,
            AverageSessionDuration = TimeSpan.FromSeconds(engRow.AvgSessionDurationSeconds),
            ClicksPerSession = totalSessions > 0
                ? Math.Round((double)engRow.TotalClicksForEngagement / totalSessions, 2)
                : 0
        };
    }

    public async Task<GeographicalStats> GetUrlGeographyAsync(
        long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT q.name, q.clicks, q.is_other, q.total_distinct
                           FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN name ELSE 'Other' END AS name,
                                   SUM(clicks) AS clicks,
                                   CASE WHEN MIN(rn) <= 5 THEN false ELSE true END AS is_other,
                                   MAX(total_distinct) AS total_distinct
                               FROM (
                                   SELECT
                                       COALESCE(NULLIF(ce.country, ''), 'Unknown') AS name,
                                       COUNT(*) AS clicks,
                                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn,
                                       COUNT(*) OVER () AS total_distinct
                                   FROM click_events ce
                                   CROSS JOIN params p
                                   WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                                   GROUP BY ce.country
                               ) Ranked
                               GROUP BY CASE WHEN rn <= 5 THEN name ELSE 'Other' END
                           ) q ORDER BY q.is_other, q.clicks DESC;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT q.name, q.clicks, q.is_other, q.total_distinct
                           FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN name ELSE 'Other' END AS name,
                                   SUM(clicks) AS clicks,
                                   CASE WHEN MIN(rn) <= 5 THEN false ELSE true END AS is_other,
                                   MAX(total_distinct) AS total_distinct
                               FROM (
                                   SELECT
                                       COALESCE(NULLIF(ce.city, ''), 'Unknown') AS name,
                                       COUNT(*) AS clicks,
                                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn,
                                       COUNT(*) OVER () AS total_distinct
                                   FROM click_events ce
                                   CROSS JOIN params p
                                   WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                                   GROUP BY ce.city
                               ) Ranked
                               GROUP BY CASE WHEN rn <= 5 THEN name ELSE 'Other' END
                           ) q ORDER BY q.is_other, q.clicks DESC;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = shortUrlId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildGeoStats(
            (await multi.ReadAsync<GeoStatRow>()).ToList(),
            (await multi.ReadAsync<GeoStatRow>()).ToList());
    }

    public async Task<DeviceStats> GetUrlDeviceStatsAsync(
        long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.device_type, ''), 'Unknown') AS Name,
                                  COUNT(*) AS Clicks,
                                  COUNT(DISTINCT ce.session_id) AS UniqueUsers
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.device_type ORDER BY Clicks DESC
                           LIMIT 5;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.browser, ''), 'Unknown') AS BrowserName,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.browser ORDER BY Clicks DESC
                           LIMIT 6;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.operating_system, ''), 'Unknown') AS OsName,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.operating_system ORDER BY Clicks DESC
                           LIMIT 6;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = shortUrlId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildDeviceStats(
            (await multi.ReadAsync<DeviceRow>()).ToList(),
            (await multi.ReadAsync<BrowserRow>()).ToList(),
            (await multi.ReadAsync<OsRow>()).ToList());
    }

    public async Task<TrafficStats> GetUrlTrafficStatsAsync(
        long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.traffic_source, ''), 'Direct') AS Source,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY COALESCE(NULLIF(ce.traffic_source, ''), 'Direct') ORDER BY Clicks DESC
                           LIMIT 6;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.referrer_domain, ''), 'Direct/Typed') AS Domain,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY COALESCE(NULLIF(ce.referrer_domain, ''), 'Direct/Typed') ORDER BY Clicks DESC
                           LIMIT 10;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.utm_campaign, ''), 'Unknown') AS CampaignName,
                                  NULLIF(ce.utm_source, '') AS Source,
                                  NULLIF(ce.utm_medium, '') AS Medium,
                                  COUNT(*) AS Clicks,
                                  MIN(ce.clicked_at) AS FirstSeen,
                                  MAX(ce.clicked_at) AS LastSeen
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                             AND ce.utm_campaign IS NOT NULL AND ce.utm_campaign != ''
                           GROUP BY ce.utm_campaign, ce.utm_source, ce.utm_medium ORDER BY Clicks DESC
                           LIMIT 10;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = shortUrlId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildTrafficStats(
            (await multi.ReadAsync<TrafficSourceRow>()).ToList(),
            (await multi.ReadAsync<ReferrerRow>()).ToList(),
            (await multi.ReadAsync<CampaignRow>()).ToList());
    }

    public async Task<TimeSeriesStats> GetUrlTimeSeriesAsync(
        long shortUrlId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var bucket = ComputeBucket(startDate, endDate);
        var sql = bucket == DateBucket.Weekly
            ? BuildUrlWeeklyTimeSeriesSql()
            : BuildUrlDailyTimeSeriesSql();

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { Id = shortUrlId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildTimeSeries(
            (await multi.ReadAsync<DailyClickRow>()).ToList(),
            (await multi.ReadAsync<HourlyClickRow>()).ToList(),
            (await multi.ReadAsync<DayOfWeekClickRow>()).ToList(),
            bucket);
    }

    private static string BuildUrlDailyTimeSeriesSql()
    {
        // language=sql
        return """
               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT ce.clicked_at::date AS Date,
                      COUNT(*) AS Clicks,
                      COUNT(DISTINCT ce.session_id) AS UniqueClicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY ce.clicked_at::date ORDER BY Date;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(HOUR FROM ce.clicked_at)::int AS Hour, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(HOUR FROM ce.clicked_at) ORDER BY Hour;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(DOW FROM ce.clicked_at)::int AS DayOfWeek, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(DOW FROM ce.clicked_at) ORDER BY DayOfWeek;
               """;
    }

    private static string BuildUrlWeeklyTimeSeriesSql()
    {
        // language=sql
        return """
               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT (date_trunc('week', ce.clicked_at))::date AS Date,
                      COUNT(*) AS Clicks,
                      COUNT(DISTINCT ce.session_id) AS UniqueClicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY (date_trunc('week', ce.clicked_at))::date
               ORDER BY Date;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(HOUR FROM ce.clicked_at)::int AS Hour, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(HOUR FROM ce.clicked_at) ORDER BY Hour;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(DOW FROM ce.clicked_at)::int AS DayOfWeek, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.short_url_id = @Id AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(DOW FROM ce.clicked_at) ORDER BY DayOfWeek;
               """;
    }

    #endregion
    
    // ═════════════════════════════════════════════════════════════════════════
    // PER-USER STATISTICS
    // ═════════════════════════════════════════════════════════════════════════

#region Per-User Statistics

    public async Task<UserOverview> GetUserOverviewAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date,
                                   CURRENT_TIMESTAMP AS now_time
                           )
                           -- RS1: URL portfolio shape
                           SELECT
                               COUNT(*)::bigint AS TotalUrls,
                               COALESCE(SUM(CASE WHEN is_active = true AND (expires_at IS NULL OR expires_at > p.now_time) THEN 1 ELSE 0 END), 0)::bigint AS ActiveUrls,
                               COALESCE(SUM(CASE WHEN expires_at IS NOT NULL AND expires_at <= p.now_time THEN 1 ELSE 0 END), 0)::bigint AS ExpiredUrls,
                               COALESCE(SUM(CASE WHEN tracking_enabled = true THEN 1 ELSE 0 END), 0)::bigint AS TotalTrackedUrls,
                               MIN(created_at) AS FirstUrlCreated,
                               MAX(created_at) AS MostRecentUrlCreated
                           FROM short_urls
                           CROSS JOIN params p
                           WHERE user_id = @UserId AND owner_type = 1;

                           -- RS2: Click totals
                           SELECT
                               COUNT(*)::bigint AS TotalClicks,
                               COUNT(DISTINCT ce.session_id)::bigint AS UniqueClicks,
                               MIN(ce.clicked_at) AS FirstClickDate,
                               MAX(ce.clicked_at) AS LastClickDate
                           FROM click_events ce
                           CROSS JOIN (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ) p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date;

                           -- RS3: Time-window counters
                           SELECT
                               COALESCE(SUM(CASE WHEN ce.clicked_at::date = CURRENT_TIMESTAMP::date THEN 1 ELSE 0 END), 0)::bigint AS ClicksToday,
                               COALESCE(SUM(CASE WHEN ce.clicked_at >= (CURRENT_TIMESTAMP::date - INTERVAL '6 days') THEN 1 ELSE 0 END), 0)::bigint AS ClicksThisWeek,
                               COALESCE(SUM(CASE WHEN ce.clicked_at >= date_trunc('month', CURRENT_TIMESTAMP) THEN 1 ELSE 0 END), 0)::bigint AS ClicksThisMonth
                           FROM click_events ce
                           WHERE ce.user_id = @UserId AND ce.clicked_at >= date_trunc('month', CURRENT_TIMESTAMP);

                           -- RS4a: Top country
                           SELECT COALESCE(NULLIF(ce.country, ''), 'Unknown') AS Name
                           FROM click_events ce
                           CROSS JOIN (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ) p
                           WHERE ce.user_id = @UserId
                             AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                             AND ce.country IS NOT NULL AND ce.country != ''
                           GROUP BY ce.country
                           ORDER BY COUNT(*) DESC
                           LIMIT 1;

                           -- RS4b: Top referrer
                           SELECT COALESCE(NULLIF(ce.referrer_domain, ''), 'Direct') AS Name
                           FROM click_events ce
                           CROSS JOIN (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ) p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.referrer_domain
                           ORDER BY COUNT(*) DESC
                           LIMIT 1;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var sw = Stopwatch.StartNew();
        
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        var (totalUrls, activeUrls, expiredUrls, totalTrackedUrls,
                firstUrlCreated, mostRecentUrlCreated) =
            await multi.ReadSingleAsync<UrlStatsRow>();

        var (totalClicks, uniqueClicks, firstClickDate, lastClickDate)
            = await multi.ReadSingleAsync<ClickTotalsRow>();

        var timeWindows = await multi.ReadSingleAsync<TimeWindowRow>();
        var topCountry = await multi.ReadFirstOrDefaultAsync<string>();
        var topReferrer = await multi.ReadFirstOrDefaultAsync<string>();
        
        sw.Stop();
        logger.LogInformation("SQL QueryMultiple = {Time} ms", sw.ElapsedMilliseconds);

        return new UserOverview
        {
            Overview = new UserOverviewMetrics
            {
                TotalUrls = totalUrls,
                ActiveUrls = activeUrls,
                ExpiredUrls = expiredUrls,
                TotalTrackedUrls = totalTrackedUrls,
                FirstUrlCreated = firstUrlCreated,
                MostRecentUrlCreated = mostRecentUrlCreated,
                TotalClicks = totalClicks,
                UniqueClicks = uniqueClicks,
                AverageClicksPerUrl = totalUrls > 0
                    ? Math.Round((double)totalClicks / totalUrls, 2)
                    : 0,
                AverageClicksPerDay = ComputeAvgClicksPerDay(
                    totalClicks, firstClickDate, lastClickDate),
                FirstClickDate = firstClickDate,
                LastClickDate = lastClickDate,
                ClicksToday = timeWindows.ClicksToday,
                ClicksThisWeek = timeWindows.ClicksThisWeek,
                ClicksThisMonth = timeWindows.ClicksThisMonth
            },
            TopCountry = topCountry,
            TopReferrer = topReferrer
        };
    }

    public async Task<EngagementMetrics> GetUserEngagementAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           ),
                           SessionStats AS (
                               SELECT
                                   ce.session_id,
                                   COUNT(*) AS SessionClicks,
                                   EXTRACT(EPOCH FROM (MAX(ce.clicked_at) - MIN(ce.clicked_at)))::double precision AS DurationSeconds
                               FROM click_events ce
                               CROSS JOIN params p
                               WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                               GROUP BY ce.session_id
                           )
                           SELECT
                               COUNT(*)::bigint AS TotalSessions,
                               COALESCE(SUM(SessionClicks), 0)::bigint AS TotalClicksForEngagement,
                               COALESCE(SUM(CASE WHEN SessionClicks = 1 THEN 1 ELSE 0 END), 0)::bigint AS BouncedSessions,
                               COALESCE(SUM(CASE WHEN SessionClicks > 1 THEN 1 ELSE 0 END), 0)::bigint AS ReturningSessions,
                               COALESCE(AVG(CASE WHEN SessionClicks > 1 THEN DurationSeconds END), 0.0) AS AvgSessionDurationSeconds
                           FROM SessionStats;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var engRow = await connection.QuerySingleAsync<EngagementRow>(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        var totalSessions = engRow.TotalSessions;

        return new EngagementMetrics
        {
            BounceRate = totalSessions > 0
                ? Math.Round((double)engRow.BouncedSessions / totalSessions * 100, 2)
                : 0,
            ReturnVisitorRate = totalSessions > 0
                ? Math.Round((double)engRow.ReturningSessions / totalSessions * 100, 2)
                : 0,
            NewVisitors = engRow.BouncedSessions,
            ReturningVisitors = engRow.ReturningSessions,
            AverageSessionDuration = TimeSpan.FromSeconds(engRow.AvgSessionDurationSeconds),
            ClicksPerSession = totalSessions > 0
                ? Math.Round((double)engRow.TotalClicksForEngagement / totalSessions, 2)
                : 0
        };
    }

    public async Task<UserTopUrls> GetUserTopUrlsAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, int limit = 10, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT 
                               su.id AS ShortUrlId,
                               su.title AS Title,
                               su.short_code AS ShortCode,
                               su.original_url AS OriginalUrl,
                               COUNT(ce.id) AS TotalClicks,
                               COUNT(DISTINCT ce.session_id) AS UniqueClicks
                           FROM short_urls su
                           LEFT JOIN click_events ce ON ce.short_url_id = su.id 
                               AND ce.clicked_at BETWEEN (SELECT start_date FROM params) AND (SELECT end_date FROM params)
                           WHERE su.user_id = @UserId AND su.owner_type = 1
                           GROUP BY su.id, su.title, su.short_code, su.original_url
                           ORDER BY TotalClicks DESC
                           LIMIT @Limit;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        var urls = await connection.QueryAsync<TopUrlRow>(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate, Limit = limit },
                cancellationToken: ct));

        return new UserTopUrls
        {
            TopUrls = urls.Select(r => new TopPerformingUrl
            {
                ShortUrlId = r.ShortUrlId,
                ShortCode = r.ShortCode,
                OriginalUrl = r.OriginalUrl,
                TotalClicks = r.TotalClicks,
                CreatedAt = r.CreatedAt,
                LastClickAt = r.LastClickAt,
                TopCountry = r.TopCountry
            }).ToList()
        };
    }

    public async Task<GeographicalStats> GetUserGeographyAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT q.name, q.clicks, q.is_other, q.total_distinct
                           FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN name ELSE 'Other' END AS name,
                                   SUM(clicks) AS clicks,
                                   CASE WHEN MIN(rn) <= 5 THEN false ELSE true END AS is_other,
                                   MAX(total_distinct) AS total_distinct
                               FROM (
                                   SELECT
                                       COALESCE(NULLIF(ce.country, ''), 'Unknown') AS name,
                                       COUNT(*) AS clicks,
                                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn,
                                       COUNT(*) OVER () AS total_distinct
                                   FROM click_events ce
                                   CROSS JOIN params p
                                   WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                                   GROUP BY ce.country
                               ) Ranked
                               GROUP BY CASE WHEN rn <= 5 THEN name ELSE 'Other' END
                           ) q ORDER BY q.is_other, q.clicks DESC;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT q.name, q.clicks, q.is_other, q.total_distinct
                           FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN name ELSE 'Other' END AS name,
                                   SUM(clicks) AS clicks,
                                   CASE WHEN MIN(rn) <= 5 THEN false ELSE true END AS is_other,
                                   MAX(total_distinct) AS total_distinct
                               FROM (
                                   SELECT
                                       COALESCE(NULLIF(ce.city, ''), 'Unknown') AS name,
                                       COUNT(*) AS clicks,
                                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn,
                                       COUNT(*) OVER () AS total_distinct
                                   FROM click_events ce
                                   CROSS JOIN params p
                                   WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                                   GROUP BY ce.city
                               ) Ranked
                               GROUP BY CASE WHEN rn <= 5 THEN name ELSE 'Other' END
                           ) q ORDER BY q.is_other, q.clicks DESC;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildGeoStats(
            (await multi.ReadAsync<GeoStatRow>()).ToList(),
            (await multi.ReadAsync<GeoStatRow>()).ToList());
    }

    public async Task<DeviceStats> GetUserDevicesAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.device_type, ''), 'Unknown') AS Name,
                                  COUNT(*) AS Clicks,
                                  COUNT(DISTINCT ce.session_id) AS UniqueUsers
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.device_type ORDER BY Clicks DESC
                           LIMIT 5;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.browser, ''), 'Unknown') AS BrowserName,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.browser ORDER BY Clicks DESC
                           LIMIT 6;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.operating_system, ''), 'Unknown') AS OsName,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY ce.operating_system ORDER BY Clicks DESC
                           LIMIT 6;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildDeviceStats(
            (await multi.ReadAsync<DeviceRow>()).ToList(),
            (await multi.ReadAsync<BrowserRow>()).ToList(),
            (await multi.ReadAsync<OsRow>()).ToList());
    }

    public async Task<TrafficStats> GetUserTrafficAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.traffic_source, ''), 'Direct') AS Source,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY COALESCE(NULLIF(ce.traffic_source, ''), 'Direct') ORDER BY Clicks DESC
                           LIMIT 6;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.referrer_domain, ''), 'Direct/Typed') AS Domain,
                                  COUNT(*) AS Clicks
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                           GROUP BY COALESCE(NULLIF(ce.referrer_domain, ''), 'Direct/Typed') ORDER BY Clicks DESC
                           LIMIT 10;

                           WITH params AS (
                               SELECT 
                                   COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                                   COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
                           )
                           SELECT COALESCE(NULLIF(ce.utm_campaign, ''), 'Unknown') AS CampaignName,
                                  NULLIF(ce.utm_source, '') AS Source,
                                  NULLIF(ce.utm_medium, '') AS Medium,
                                  COUNT(*) AS Clicks,
                                  MIN(ce.clicked_at) AS FirstSeen,
                                  MAX(ce.clicked_at) AS LastSeen
                           FROM click_events ce
                           CROSS JOIN params p
                           WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
                             AND ce.utm_campaign IS NOT NULL AND ce.utm_campaign != ''
                           GROUP BY ce.utm_campaign, ce.utm_source, ce.utm_medium ORDER BY Clicks DESC
                           LIMIT 10;
                           """;

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildTrafficStats(
            (await multi.ReadAsync<TrafficSourceRow>()).ToList(),
            (await multi.ReadAsync<ReferrerRow>()).ToList(),
            (await multi.ReadAsync<CampaignRow>()).ToList());
    }

    public async Task<TimeSeriesStats> GetUserTimeSeriesAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var bucket = ComputeBucket(startDate, endDate);
        var sql = bucket == DateBucket.Weekly
            ? BuildUserWeeklyTimeSeriesSql()
            : BuildUserDailyTimeSeriesSql();

        using var connection = sqlConnectionFactory.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildTimeSeries(
            (await multi.ReadAsync<DailyClickRow>()).ToList(),
            (await multi.ReadAsync<HourlyClickRow>()).ToList(),
            (await multi.ReadAsync<DayOfWeekClickRow>()).ToList(),
            bucket);
    }

    private static string BuildUserDailyTimeSeriesSql()
    {
        // language=sql
        return """
               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT ce.clicked_at::date AS Date,
                      COUNT(*) AS Clicks,
                      COUNT(DISTINCT ce.session_id) AS UniqueClicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY ce.clicked_at::date ORDER BY Date;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(HOUR FROM ce.clicked_at)::int AS Hour, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(HOUR FROM ce.clicked_at) ORDER BY Hour;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(DOW FROM ce.clicked_at)::int AS DayOfWeek, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(DOW FROM ce.clicked_at) ORDER BY DayOfWeek;
               """;
    }

    private static string BuildUserWeeklyTimeSeriesSql()
    {
        // language=sql
        return """
               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT (date_trunc('week', ce.clicked_at))::date AS Date,
                      COUNT(*) AS Clicks,
                      COUNT(DISTINCT ce.session_id) AS UniqueClicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY (date_trunc('week', ce.clicked_at))::date
               ORDER BY Date;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(HOUR FROM ce.clicked_at)::int AS Hour, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(HOUR FROM ce.clicked_at) ORDER BY Hour;

               WITH params AS (
                   SELECT 
                       COALESCE(@E, CURRENT_TIMESTAMP) AS end_date,
                       COALESCE(@S, COALESCE(@E, CURRENT_TIMESTAMP) - INTERVAL '30 days') AS start_date
               )
               SELECT EXTRACT(DOW FROM ce.clicked_at)::int AS DayOfWeek, COUNT(*) AS Clicks
               FROM click_events ce
               CROSS JOIN params p
               WHERE ce.user_id = @UserId AND ce.clicked_at BETWEEN p.start_date AND p.end_date
               GROUP BY EXTRACT(DOW FROM ce.clicked_at) ORDER BY DayOfWeek;
               """;
    }

    public async Task<UserTopUrls> GetUserTopUrlsAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        return await GetUserTopUrlsAsync(userId, startDate, endDate, limit: 10, ct);
    }
    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    #region Private Helpers

    private static GeographicalStats BuildGeoStats(
        IReadOnlyList<GeoStatRow> countryRows,
        IReadOnlyList<GeoStatRow> cityRows)
    {
        return new GeographicalStats
        {
            TopCountries = countryRows.Select(r => new StatItem(r.Name, r.Clicks)).ToList(),
            TopCities = cityRows.Select(r => new StatItem(r.Name, r.Clicks)).ToList(),
            TotalCountries = countryRows.Count > 0 ? countryRows[0].TotalDistinct : 0,
            TotalCities = cityRows.Count > 0 ? cityRows[0].TotalDistinct : 0
        };
    }

    private static double ComputeAvgClicksPerDay(long totalClicks, DateTime? firstClick, DateTime? lastClick)
    {
        if (totalClicks == 0 || firstClick is null || lastClick is null) return 0;
        var days = Math.Max(1, (lastClick.Value.Date - firstClick.Value.Date).TotalDays + 1);
        return Math.Round(totalClicks / days, 2);
    }

    private static TimeSeriesStats BuildTimeSeries(
        List<DailyClickRow> dailyRows,
        List<HourlyClickRow> hourlyRows,
        List<DayOfWeekClickRow> dowRows,
        DateBucket bucket)
    {
        var dailyDict = dailyRows.ToDictionary(r => r.Date, r => r.Clicks);
        var hourlyDict = hourlyRows.ToDictionary(r => r.Hour, r => r.Clicks);
        var dowDict = dowRows.ToDictionary(r => (DayOfWeek)r.DayOfWeek, r => r.Clicks);

        var totalClicks = dailyRows.Sum(r => (long)r.Clicks);
        var activeDays = dailyRows.Count;
        var activeHours = hourlyRows.Count;
        var peakDayRow = dailyRows.MaxBy(r => r.Clicks);
        var peakHourRow = hourlyRows.MaxBy(r => r.Clicks);
        var peakDowRow = dowRows.MaxBy(r => r.Clicks);

        return new TimeSeriesStats
        {
            BucketType = bucket == DateBucket.Weekly ? "weekly" : "daily",
            DailyClicks = dailyDict,
            HourlyDistribution = hourlyDict,
            ClicksByHourOfDay = hourlyDict,
            ClicksByDayOfWeek = dowDict,
            Trend = dailyRows.Select(r => new TimeSeriesDataPoint
            {
                Timestamp = r.Date,
                Clicks = r.Clicks,
                UniqueClicks = r.UniqueClicks
            }).ToList(),
            PeakHour = peakHourRow?.Hour ?? 0,
            PeakDay = peakDowRow != null ? (DayOfWeek)peakDowRow.DayOfWeek : DayOfWeek.Sunday,
            PeakDate = peakDayRow?.Date,
            PeakDateClicks = peakDayRow?.Clicks ?? 0,
            AverageClicksPerDay = activeDays > 0 ? Math.Round((double)totalClicks / activeDays, 2) : 0,
            AverageClicksPerHour = activeHours > 0 ? Math.Round((double)totalClicks / activeHours, 2) : 0
        };
    }

    private static TrafficStats BuildTrafficStats(
        List<TrafficSourceRow> sourceRows,
        List<ReferrerRow> referrerRows,
        List<CampaignRow> campaignRows)
    {
        var sourceTotal = sourceRows.Sum(r => r.Clicks);
        var referrerTotal = referrerRows.Sum(r => r.Clicks);
        var campaignTotal = campaignRows.Sum(r => r.Clicks);

        double SourcePct(string name)
        {
            return sourceTotal > 0
                ? Math.Round(sourceRows.Where(r => r.Source == name).Sum(r => r.Clicks) / (double)sourceTotal * 100, 2)
                : 0;
        }

        return new TrafficStats
        {
            TopTrafficSources = sourceRows.Select(r => new TrafficSourceBreakdown
            {
                Source = r.Source,
                Clicks = r.Clicks,
                Percentage = sourceTotal > 0 ? Math.Round(r.Clicks / (double)sourceTotal * 100, 2) : 0
            }).ToList(),
            TopReferrers = referrerRows.Select(r => new ReferrerBreakdown
            {
                Domain = r.Domain,
                Clicks = r.Clicks,
                Percentage = referrerTotal > 0 ? Math.Round(r.Clicks / (double)referrerTotal * 100, 2) : 0
            }).ToList(),
            TopCampaigns = campaignRows.Select(r => new CampaignPerformance
            {
                CampaignName = r.CampaignName ?? "Unknown",
                Source = r.Source,
                Medium = r.Medium,
                Clicks = r.Clicks,
                FirstSeen = r.FirstSeen,
                LastSeen = r.LastSeen,
                Percentage = campaignTotal > 0 ? Math.Round(r.Clicks / (double)campaignTotal * 100, 2) : 0
            }).ToList(),
            DirectTrafficPercentage = SourcePct("Direct"),
            SearchTrafficPercentage = SourcePct("Search"),
            SocialTrafficPercentage = SourcePct("Social"),
            ReferralTrafficPercentage = SourcePct("Referral")
        };
    }

    private static DeviceStats BuildDeviceStats(
        List<DeviceRow> deviceRows,
        List<BrowserRow> browserRows,
        List<OsRow> osRows)
    {
        var deviceTotal = deviceRows.Sum(r => r.Clicks);
        var browserTotal = browserRows.Sum(r => r.Clicks);
        var osTotal = osRows.Sum(r => r.Clicks);

        double DevicePct(string name)
        {
            return deviceTotal > 0
                ? Math.Round(deviceRows.Where(r => r.Name == name).Sum(r => r.Clicks) / (double)deviceTotal * 100, 2)
                : 0;
        }

        return new DeviceStats
        {
            TopDeviceTypes = deviceRows.Select(r => new DeviceBreakdown
            {
                Name = r.Name,
                Clicks = r.Clicks,
                UniqueUsers = r.UniqueUsers,
                Percentage = deviceTotal > 0 ? Math.Round(r.Clicks / (double)deviceTotal * 100, 2) : 0
            }).ToList(),
            TopBrowsers = browserRows.Select(r => new BrowserBreakdown
            {
                BrowserName = r.BrowserName,
                Clicks = r.Clicks,
                Percentage = browserTotal > 0 ? Math.Round(r.Clicks / (double)browserTotal * 100, 2) : 0
            }).ToList(),
            TopOperatingSystems = osRows.Select(r => new OsBreakdown
            {
                OsName = r.OsName,
                Clicks = r.Clicks,
                Percentage = osTotal > 0 ? Math.Round(r.Clicks / (double)osTotal * 100, 2) : 0
            }).ToList(),
            MobilePercentage = DevicePct("Mobile"),
            DesktopPercentage = DevicePct("Desktop"),
            TabletPercentage = DevicePct("Tablet")
        };
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    // PROJECTION RECORDS
    // ═════════════════════════════════════════════════════════════════════════

    #region Projection Records

    private record GeoStatRow(string Name, int Clicks, bool IsOther, int TotalDistinct);

    private record UrlStatsRow(
        int TotalUrls,
        int ActiveUrls,
        int ExpiredUrls,
        int TotalTrackedUrls,
        DateTime? FirstUrlCreated,
        DateTime? MostRecentUrlCreated);

    private record ClickTotalsRow(
        long TotalClicks,
        long UniqueClicks,
        DateTime? FirstClickDate,
        DateTime? LastClickDate);

    private record TimeWindowRow(long ClicksToday, long ClicksThisWeek, long ClicksThisMonth);

    private record CoreMetricsRow(
        long TotalClicks,
        long UniqueClicks,
        int ActiveDays,
        DateTime? FirstClickDate,
        DateTime? LastClickDate);

    private record EngagementRow(
        int TotalSessions,
        long TotalClicksForEngagement,
        int BouncedSessions,
        int ReturningSessions,
        double AvgSessionDurationSeconds);

    private class TopUrlRow
    {
        public long ShortUrlId { get; set; }
        public string ShortCode { get; set; }
        public string OriginalUrl { get; set; }
        public long TotalClicks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastClickAt { get; set; }
        public string? TopCountry { get; set; }
    }

    private record DailyClickRow(DateTime Date, int Clicks, int UniqueClicks);

    private record HourlyClickRow(int Hour, int Clicks);

    private record DayOfWeekClickRow(int DayOfWeek, int Clicks);

    private record TrafficSourceRow(string Source, int Clicks);

    private record ReferrerRow(string Domain, int Clicks);

    private record CampaignRow(
        string? CampaignName,
        string? Source,
        string? Medium,
        int Clicks,
        DateTime FirstSeen,
        DateTime LastSeen);

    private record DeviceRow(string Name, int Clicks, int UniqueUsers);

    private record BrowserRow(string BrowserName, int Clicks);

    private record OsRow(string OsName, int Clicks);

    #endregion
}