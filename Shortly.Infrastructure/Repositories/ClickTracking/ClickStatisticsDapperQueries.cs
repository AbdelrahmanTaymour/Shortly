using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Analytics.DTOs;

namespace Shortly.Infrastructure.Repositories.ClickTracking;

/// <summary>
///     Dapper query implementation for analytics.
/// </summary>
public class ClickStatisticsDapperQueries(IConfiguration configuration) : IClickStatisticsDapperQueries
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not found.");

    private SqlConnection OpenConnection()
    {
        return new SqlConnection(_connectionString);
    }

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
                           DECLARE @End     DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start   DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));
                           DECLARE @Now     DATETIME2 = GETUTCDATE();
                           DECLARE @Today   DATE      = CAST(@Now AS DATE);
                           DECLARE @WkStart DATE      = DATEADD(DAY, -6, @Today);
                           DECLARE @MoStart DATE      = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);

                           -- RS1: Core click metrics (date-range scoped).
                           -- COUNT_BIG + COUNT_BIG(DISTINCT) — index seek on (ShortUrlId, ClickedAt), no grouping.
                           SELECT COUNT_BIG(*)                               AS TotalClicks,
                                  COUNT_BIG(DISTINCT ce.SessionId)           AS UniqueClicks,
                                  COUNT(DISTINCT CAST(ce.ClickedAt AS DATE)) AS ActiveDays,
                                  MIN(ce.ClickedAt)                          AS FirstClickDate,
                                  MAX(ce.ClickedAt)                          AS LastClickDate
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End;

                           -- RS2: Time-window counters.
                           -- @MoStart cap keeps this scan ≤ 30 days regardless of @Start.
                           SELECT ISNULL(CAST(SUM(CASE WHEN CAST(ce.ClickedAt AS DATE) = @Today THEN 1 ELSE 0 END) AS BIGINT), 0) AS ClicksToday,
                                  ISNULL(CAST(SUM(CASE WHEN ce.ClickedAt >= @WkStart             THEN 1 ELSE 0 END) AS BIGINT), 0) AS ClicksThisWeek,
                                  ISNULL(CAST(SUM(CASE WHEN ce.ClickedAt >= @MoStart             THEN 1 ELSE 0 END) AS BIGINT), 0) AS ClicksThisMonth
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt >= @MoStart;

                           -- RS3: Top country — TOP 1, one GROUP BY, one row returned.
                           SELECT TOP 1 ISNULL(NULLIF(ce.Country, ''), 'Unknown') AS Name
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                             AND ce.Country IS NOT NULL AND ce.Country != ''
                           GROUP BY ce.Country
                           ORDER BY COUNT(*) DESC;

                           -- RS4: Top referrer domain — TOP 1, one GROUP BY, one row returned.
                           SELECT TOP 1 ISNULL(NULLIF(ce.ReferrerDomain, ''), 'Direct') AS Name
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.ReferrerDomain
                           ORDER BY COUNT(*) DESC;
                           """;

        await using var connection = OpenConnection();
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
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           -- Full GROUP BY SessionId over the date-range partition for this URL.
                           -- Deferred intentionally — this is the expensive operation.
                           WITH SessionStats AS (
                               SELECT
                                   ce.SessionId,
                                   COUNT(*)                                                AS SessionClicks,
                                   DATEDIFF(SECOND, MIN(ce.ClickedAt), MAX(ce.ClickedAt)) AS DurationSeconds
                               FROM ClickEvents ce WITH (NOLOCK)
                               WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                               GROUP BY ce.SessionId
                           )
                           SELECT
                               COUNT(*)                                                                        AS TotalSessions,
                               CAST(ISNULL(SUM(SessionClicks), 0) AS BIGINT)                                  AS TotalClicksForEngagement,
                               ISNULL(SUM(CASE WHEN SessionClicks = 1 THEN 1 ELSE 0 END), 0)                  AS BouncedSessions,
                               ISNULL(SUM(CASE WHEN SessionClicks > 1 THEN 1 ELSE 0 END), 0)                  AS ReturningSessions,
                               ISNULL(AVG(CASE WHEN SessionClicks > 1 THEN CAST(DurationSeconds AS FLOAT) END), 0.0)
                                   AS AvgSessionDurationSeconds
                           FROM SessionStats;
                           """;

        await using var connection = OpenConnection();
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
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           SELECT q.Name, q.Clicks, q.IsOther, q.TotalDistinct
                           FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN Name ELSE 'Other' END          AS Name,
                                   SUM(Clicks)                                            AS Clicks,
                                   CAST(CASE WHEN MIN(rn) <= 5 THEN 0 ELSE 1 END AS BIT) AS IsOther,
                                   MAX(TotalDistinct)                                     AS TotalDistinct
                               FROM (
                                   SELECT
                                       ISNULL(NULLIF(ce.Country, ''), 'Unknown')    AS Name,
                                       COUNT(*)                                      AS Clicks,
                                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC)   AS rn,
                                       COUNT(*) OVER ()                             AS TotalDistinct
                                   FROM ClickEvents ce WITH (NOLOCK)
                                   WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                                   GROUP BY ce.Country
                               ) Ranked
                               GROUP BY CASE WHEN rn <= 5 THEN Name ELSE 'Other' END
                           ) q ORDER BY q.IsOther, q.Clicks DESC;

                           SELECT q.Name, q.Clicks, q.IsOther, q.TotalDistinct
                           FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN Name ELSE 'Other' END          AS Name,
                                   SUM(Clicks)                                            AS Clicks,
                                   CAST(CASE WHEN MIN(rn) <= 5 THEN 0 ELSE 1 END AS BIT) AS IsOther,
                                   MAX(TotalDistinct)                                     AS TotalDistinct
                               FROM (
                                   SELECT
                                       ISNULL(NULLIF(ce.City, ''), 'Unknown')       AS Name,
                                       COUNT(*)                                      AS Clicks,
                                       ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC)   AS rn,
                                       COUNT(*) OVER ()                             AS TotalDistinct
                                   FROM ClickEvents ce WITH (NOLOCK)
                                   WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                                   GROUP BY ce.City
                               ) Ranked
                               GROUP BY CASE WHEN rn <= 5 THEN Name ELSE 'Other' END
                           ) q ORDER BY q.IsOther, q.Clicks DESC;
                           """;

        await using var connection = OpenConnection();
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
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           SELECT TOP 5
                               ISNULL(NULLIF(ce.DeviceType, ''), 'Unknown') AS Name,
                               COUNT(*)                                      AS Clicks,
                               COUNT(DISTINCT ce.SessionId)                  AS UniqueUsers
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.DeviceType ORDER BY Clicks DESC;

                           SELECT TOP 6
                               ISNULL(NULLIF(ce.Browser, ''), 'Unknown') AS BrowserName,
                               COUNT(*)                                   AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.Browser ORDER BY Clicks DESC;

                           SELECT TOP 6
                               ISNULL(NULLIF(ce.OperatingSystem, ''), 'Unknown') AS OsName,
                               COUNT(*)                                           AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.OperatingSystem ORDER BY Clicks DESC;
                           """;

        await using var connection = OpenConnection();
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
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           SELECT TOP 6
                               ISNULL(NULLIF(ce.TrafficSource, ''), 'Direct') AS Source,
                               COUNT(*) AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ISNULL(NULLIF(ce.TrafficSource, ''), 'Direct') ORDER BY Clicks DESC;

                           SELECT TOP 10
                               ISNULL(NULLIF(ce.ReferrerDomain, ''), 'Direct/Typed') AS Domain,
                               COUNT(*) AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ISNULL(NULLIF(ce.ReferrerDomain, ''), 'Direct/Typed') ORDER BY Clicks DESC;

                           SELECT TOP 10
                               ISNULL(NULLIF(ce.UtmCampaign, ''), 'Unknown') AS CampaignName,
                               NULLIF(ce.UtmSource, '')  AS Source,
                               NULLIF(ce.UtmMedium, '')  AS Medium,
                               COUNT(*)                  AS Clicks,
                               MIN(ce.ClickedAt)         AS FirstSeen,
                               MAX(ce.ClickedAt)         AS LastSeen
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
                             AND ce.UtmCampaign IS NOT NULL AND ce.UtmCampaign != ''
                           GROUP BY ce.UtmCampaign, ce.UtmSource, ce.UtmMedium ORDER BY Clicks DESC;
                           """;

        await using var connection = OpenConnection();
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

        await using var connection = OpenConnection();
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
               DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
               DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

               SELECT CAST(ce.ClickedAt AS DATE) AS Date,
                      COUNT(*)                  AS Clicks,
                      COUNT(DISTINCT ce.SessionId) AS UniqueClicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY CAST(ce.ClickedAt AS DATE) ORDER BY Date;

               SELECT DATEPART(HOUR, ce.ClickedAt) AS Hour, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(HOUR, ce.ClickedAt) ORDER BY Hour;

               SELECT DATEPART(WEEKDAY, ce.ClickedAt) - 1 AS DayOfWeek, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(WEEKDAY, ce.ClickedAt) ORDER BY DayOfWeek;
               """;
    }

    private static string BuildUrlWeeklyTimeSeriesSql()
    {
        // language=sql
        return """
               DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
               DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

               SELECT DATEADD(DAY, 1 - DATEPART(WEEKDAY, CAST(ce.ClickedAt AS DATE)), CAST(ce.ClickedAt AS DATE)) AS Date,
                      COUNT(*)                     AS Clicks,
                      COUNT(DISTINCT ce.SessionId) AS UniqueClicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEADD(DAY, 1 - DATEPART(WEEKDAY, CAST(ce.ClickedAt AS DATE)), CAST(ce.ClickedAt AS DATE))
               ORDER BY Date;

               SELECT DATEPART(HOUR, ce.ClickedAt) AS Hour, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(HOUR, ce.ClickedAt) ORDER BY Hour;

               SELECT DATEPART(WEEKDAY, ce.ClickedAt) - 1 AS DayOfWeek, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.ShortUrlId = @Id AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(WEEKDAY, ce.ClickedAt) ORDER BY DayOfWeek;
               """;
    }

    #endregion

    // ═════════════════════════════════════════════════════════════════════════
    // PER-USER STATISTICS
    // ═════════════════════════════════════════════════════════════════════════

    #region Per-User Statistics

    // ── OVERVIEW (lightweight — no session grouping, no URL ranking) ──────────

    public async Task<UserOverview> GetUserOverviewAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           DECLARE @End     DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start   DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));
                           DECLARE @Now     DATETIME2 = GETUTCDATE();
                           DECLARE @Today   DATE      = CAST(@Now AS DATE);
                           DECLARE @WkStart DATE      = DATEADD(DAY, -6, @Today);
                           DECLARE @MoStart DATE      = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);

                           -- ── Result set 1: URL portfolio shape ────────────────────────────
                           SELECT
                               COUNT(*)                                                                              AS TotalUrls,
                               ISNULL(SUM(CASE WHEN IsActive = 1 AND (ExpiresAt IS NULL OR ExpiresAt > @Now) THEN 1 ELSE 0 END), 0) AS ActiveUrls,
                               ISNULL(SUM(CASE WHEN ExpiresAt IS NOT NULL AND ExpiresAt <= @Now THEN 1 ELSE 0 END), 0)              AS ExpiredUrls,
                               ISNULL(SUM(CASE WHEN TrackingEnabled = 1 THEN 1 ELSE 0 END), 0)                                      AS TotalTrackedUrls,
                               MIN(CreatedAt)                                                                        AS FirstUrlCreated,
                               MAX(CreatedAt)                                                                        AS MostRecentUrlCreated
                           FROM ShortUrls WITH (NOLOCK)
                           WHERE UserId = @UserId AND OwnerType = 1;

                           -- ── Result set 2: Click totals (date-range scoped) ───────────────
                           -- Index seek on (UserId, ClickedAt).
                           SELECT
                               COUNT_BIG(*)                     AS TotalClicks,
                               COUNT_BIG(DISTINCT ce.SessionId) AS UniqueClicks,
                               MIN(ce.ClickedAt)                AS FirstClickDate,
                               MAX(ce.ClickedAt)                AS LastClickDate
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End;

                           -- ── Result set 3: Time-window counters (month-scoped) ─────────────
                           -- @MoStart caps the scan to ~30 days regardless of date-range param.
                           SELECT
                               ISNULL(CAST(SUM(CASE WHEN CAST(ce.ClickedAt AS DATE) = @Today THEN 1 ELSE 0 END) AS BIGINT), 0) AS ClicksToday,
                               ISNULL(CAST(SUM(CASE WHEN ce.ClickedAt >= @WkStart             THEN 1 ELSE 0 END) AS BIGINT), 0) AS ClicksThisWeek,
                               ISNULL(CAST(SUM(CASE WHEN ce.ClickedAt >= @MoStart             THEN 1 ELSE 0 END) AS BIGINT), 0) AS ClicksThisMonth
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt >= @MoStart;

                           -- ── Result set 4a: Top country (single row) ──────────────────────
                           SELECT TOP 1
                               ISNULL(NULLIF(ce.Country, ''), 'Unknown') AS Name
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId
                             AND ce.ClickedAt BETWEEN @Start AND @End
                             AND ce.Country IS NOT NULL AND ce.Country != ''
                           GROUP BY ce.Country
                           ORDER BY COUNT(*) DESC;

                           -- ── Result set 4b: Top referrer (single row) ─────────────────────
                           SELECT TOP 1
                               ISNULL(NULLIF(ce.ReferrerDomain, ''), 'Direct') AS Name
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.ReferrerDomain
                           ORDER BY COUNT(*) DESC;
                           """;

        await using var connection = OpenConnection();
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

    // ── ENGAGEMENT (heavy — deferred until Engagement tab activated) ──────────

    public async Task<EngagementMetrics> GetUserEngagementAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           -- Full GROUP BY SessionId over the date-range partition.
                           -- This is an expensive operation deferred intentionally.
                           WITH SessionStats AS (
                               SELECT
                                   ce.SessionId,
                                   COUNT(*)                                                AS SessionClicks,
                                   DATEDIFF(SECOND, MIN(ce.ClickedAt), MAX(ce.ClickedAt)) AS DurationSeconds
                               FROM ClickEvents ce WITH (NOLOCK)
                               WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                               GROUP BY ce.SessionId
                           )
                           SELECT
                               COUNT(*)                                                                        AS TotalSessions,
                               CAST(ISNULL(SUM(SessionClicks), 0) AS BIGINT)                                  AS TotalClicksForEngagement,
                               ISNULL(SUM(CASE WHEN SessionClicks = 1 THEN 1 ELSE 0 END), 0)                  AS BouncedSessions,
                               ISNULL(SUM(CASE WHEN SessionClicks > 1 THEN 1 ELSE 0 END), 0)                  AS ReturningSessions,
                               ISNULL(AVG(CASE WHEN SessionClicks > 1 THEN CAST(DurationSeconds AS FLOAT) END), 0.0)
                                   AS AvgSessionDurationSeconds
                           FROM SessionStats;
                           """;

        await using var connection = OpenConnection();
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

    // ── TOP URLs (heavy — deferred until Top URLs tab activated) ─────────────

    public async Task<UserTopUrls> GetUserTopUrlsAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           WITH Aggregated AS (
                               SELECT 
                                   ce.ShortUrlId,
                                   ce.Country,
                                   COUNT(*) AS ClickCount,
                                   MAX(ce.ClickedAt) AS LastClickAt
                               FROM ClickEvents ce WITH (NOLOCK)
                               WHERE ce.UserId = @UserId 
                                 AND ce.ClickedAt >= ISNULL(@S, DATEADD(DAY, -30, GETUTCDATE()))
                                 AND ce.ClickedAt <  ISNULL(@E, GETUTCDATE())
                               GROUP BY ce.ShortUrlId, ce.Country
                           ),
                           TopUrls AS (
                               SELECT TOP 10
                                   ShortUrlId,
                                   SUM(ClickCount) AS TotalClicks,
                                   MAX(LastClickAt) AS LastClickAt
                               FROM Aggregated
                               GROUP BY ShortUrlId
                               ORDER BY SUM(ClickCount) DESC
                           ),
                           CountryRanked AS (
                               SELECT 
                                   a.ShortUrlId,
                                   a.Country,
                                   ROW_NUMBER() OVER (
                                       PARTITION BY a.ShortUrlId 
                                       ORDER BY a.ClickCount DESC
                                   ) AS rn
                               FROM Aggregated a
                               INNER JOIN TopUrls t ON t.ShortUrlId = a.ShortUrlId
                               WHERE a.Country IS NOT NULL AND a.Country != ''
                           )
                           SELECT
                               t.ShortUrlId,
                               su.ShortCode,
                               su.OriginalUrl,
                               t.TotalClicks,
                               su.CreatedAt,
                               t.LastClickAt,
                               cr.Country AS TopCountry
                           FROM TopUrls t
                           INNER JOIN ShortUrls su WITH (NOLOCK) ON su.Id = t.ShortUrlId
                           LEFT JOIN CountryRanked cr 
                               ON cr.ShortUrlId = t.ShortUrlId AND cr.rn = 1
                           ORDER BY t.TotalClicks DESC;
                           """;

        await using var connection = OpenConnection();

        var topUrls = (await connection.QueryAsync<TopUrlRow>(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate }, cancellationToken: ct)
        )).ToList();

        return new UserTopUrls
        {
            TopUrls = topUrls.Select(r => new TopPerformingUrl
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

    // ── Unchanged user slice queries ─────────────────────────────────────────

    public async Task<TimeSeriesStats> GetUserTimeSeriesAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var bucket = ComputeBucket(startDate, endDate);
        var sql = bucket == DateBucket.Weekly
            ? BuildUserWeeklyTimeSeriesSql()
            : BuildUserDailyTimeSeriesSql();

        await using var connection = OpenConnection();
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
               DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
               DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

               SELECT CAST(ce.ClickedAt AS DATE)   AS Date,
                      COUNT(*)                     AS Clicks,
                      COUNT(DISTINCT ce.SessionId) AS UniqueClicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY CAST(ce.ClickedAt AS DATE) ORDER BY Date;

               SELECT DATEPART(HOUR, ce.ClickedAt) AS Hour, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(HOUR, ce.ClickedAt) ORDER BY Hour;

               SELECT DATEPART(WEEKDAY, ce.ClickedAt) - 1 AS DayOfWeek, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(WEEKDAY, ce.ClickedAt) ORDER BY DayOfWeek;
               """;
    }

    private static string BuildUserWeeklyTimeSeriesSql()
    {
        // language=sql
        return """
               DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
               DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

               SELECT DATEADD(DAY, 1 - DATEPART(WEEKDAY, CAST(ce.ClickedAt AS DATE)), CAST(ce.ClickedAt AS DATE)) AS Date,
                      COUNT(*)                     AS Clicks,
                      COUNT(DISTINCT ce.SessionId) AS UniqueClicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEADD(DAY, 1 - DATEPART(WEEKDAY, CAST(ce.ClickedAt AS DATE)), CAST(ce.ClickedAt AS DATE))
               ORDER BY Date;

               SELECT DATEPART(HOUR, ce.ClickedAt) AS Hour, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(HOUR, ce.ClickedAt) ORDER BY Hour;

               SELECT DATEPART(WEEKDAY, ce.ClickedAt) - 1 AS DayOfWeek, COUNT(*) AS Clicks
               FROM ClickEvents ce WITH (NOLOCK)
               WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
               GROUP BY DATEPART(WEEKDAY, ce.ClickedAt) ORDER BY DayOfWeek;
               """;
    }

    public async Task<GeographicalStats> GetUserGeographyAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           SELECT q.Name, q.Clicks, q.IsOther, q.TotalDistinct FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN Name ELSE 'Other' END          AS Name,
                                   SUM(Clicks)                                            AS Clicks,
                                   CAST(CASE WHEN MIN(rn) <= 5 THEN 0 ELSE 1 END AS BIT) AS IsOther,
                                   MAX(TotalDistinct)                                     AS TotalDistinct
                               FROM (
                                   SELECT ISNULL(NULLIF(ce.Country, ''), 'Unknown') AS Name, COUNT(*) AS Clicks,
                                          ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn, COUNT(*) OVER () AS TotalDistinct
                                   FROM ClickEvents ce WITH (NOLOCK)
                                   WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                                   GROUP BY ce.Country
                               ) r GROUP BY CASE WHEN rn <= 5 THEN Name ELSE 'Other' END
                           ) q ORDER BY q.IsOther, q.Clicks DESC;

                           SELECT q.Name, q.Clicks, q.IsOther, q.TotalDistinct FROM (
                               SELECT
                                   CASE WHEN rn <= 5 THEN Name ELSE 'Other' END          AS Name,
                                   SUM(Clicks)                                            AS Clicks,
                                   CAST(CASE WHEN MIN(rn) <= 5 THEN 0 ELSE 1 END AS BIT) AS IsOther,
                                   MAX(TotalDistinct)                                     AS TotalDistinct
                               FROM (
                                   SELECT ISNULL(NULLIF(ce.City, ''), 'Unknown') AS Name, COUNT(*) AS Clicks,
                                          ROW_NUMBER() OVER (ORDER BY COUNT(*) DESC) AS rn, COUNT(*) OVER () AS TotalDistinct
                                   FROM ClickEvents ce WITH (NOLOCK)
                                   WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                                   GROUP BY ce.City
                               ) r GROUP BY CASE WHEN rn <= 5 THEN Name ELSE 'Other' END
                           ) q ORDER BY q.IsOther, q.Clicks DESC;
                           """;

        await using var connection = OpenConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildGeoStats(
            (await multi.ReadAsync<GeoStatRow>()).ToList(),
            (await multi.ReadAsync<GeoStatRow>()).ToList());
    }

    public async Task<TrafficStats> GetUserTrafficAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           SELECT TOP 6
                               ISNULL(NULLIF(ce.TrafficSource, ''), 'Direct') AS Source, COUNT(*) AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ISNULL(NULLIF(ce.TrafficSource, ''), 'Direct') ORDER BY Clicks DESC;

                           SELECT TOP 10
                               ISNULL(NULLIF(ce.ReferrerDomain, ''), 'Direct/Typed') AS Domain, COUNT(*) AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ISNULL(NULLIF(ce.ReferrerDomain, ''), 'Direct/Typed') ORDER BY Clicks DESC;

                           SELECT TOP 10
                               ISNULL(NULLIF(ce.UtmCampaign, ''), 'Unknown') AS CampaignName,
                               NULLIF(ce.UtmSource, '') AS Source, NULLIF(ce.UtmMedium, '') AS Medium,
                               COUNT(*) AS Clicks, MIN(ce.ClickedAt) AS FirstSeen, MAX(ce.ClickedAt) AS LastSeen
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                             AND ce.UtmCampaign IS NOT NULL AND ce.UtmCampaign != ''
                           GROUP BY ce.UtmCampaign, ce.UtmSource, ce.UtmMedium ORDER BY Clicks DESC;
                           """;

        await using var connection = OpenConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildTrafficStats(
            (await multi.ReadAsync<TrafficSourceRow>()).ToList(),
            (await multi.ReadAsync<ReferrerRow>()).ToList(),
            (await multi.ReadAsync<CampaignRow>()).ToList());
    }

    public async Task<DeviceStats> GetUserDevicesAsync(
        Guid userId, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        // language=sql
        const string sql = """
                           DECLARE @End   DATETIME2 = ISNULL(@E, GETUTCDATE());
                           DECLARE @Start DATETIME2 = ISNULL(@S, DATEADD(DAY, -30, @End));

                           SELECT TOP 5
                               ISNULL(NULLIF(ce.DeviceType, ''), 'Unknown') AS Name,
                               COUNT(*) AS Clicks, COUNT(DISTINCT ce.SessionId) AS UniqueUsers
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.DeviceType ORDER BY Clicks DESC;

                           SELECT TOP 6
                               ISNULL(NULLIF(ce.Browser, ''), 'Unknown') AS BrowserName, COUNT(*) AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.Browser ORDER BY Clicks DESC;

                           SELECT TOP 6
                               ISNULL(NULLIF(ce.OperatingSystem, ''), 'Unknown') AS OsName, COUNT(*) AS Clicks
                           FROM ClickEvents ce WITH (NOLOCK)
                           WHERE ce.UserId = @UserId AND ce.ClickedAt BETWEEN @Start AND @End
                           GROUP BY ce.OperatingSystem ORDER BY Clicks DESC;
                           """;

        await using var connection = OpenConnection();
        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { UserId = userId, S = startDate, E = endDate },
                cancellationToken: ct));

        return BuildDeviceStats(
            (await multi.ReadAsync<DeviceRow>()).ToList(),
            (await multi.ReadAsync<BrowserRow>()).ToList(),
            (await multi.ReadAsync<OsRow>()).ToList());
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