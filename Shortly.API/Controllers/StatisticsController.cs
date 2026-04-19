using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.Analytics.Contracts;
using Shortly.Core.Analytics.DTOs;
using Shortly.Core.Analytics.Services;
using Shortly.Core.Common.Abstractions;
using Shortly.Core.Exceptions.DTOs;

namespace Shortly.API.Controllers;

/// <summary>
///     Optimized analytics controller for URL and user statistics.
///
/// ─── DATE RANGE POLICY ────────────────────────────────────────────────────────
/// <remarks>
///     All endpoints accept optional <c>startDate</c> / <c>endDate</c> query params.
///     • If both are omitted, the service defaults to the last 30 days.
///     • If only <c>endDate</c> is omitted, it defaults to now.
///     • The maximum allowable range is 365 days. Wider ranges are clamped
///       silently in <see cref="UrlStatisticsService"/> so that the "all time"
///       frontend preset degrades gracefully rather than timing out.
///     • Ranges where <c>startDate >= endDate</c> are rejected with HTTP 400.
/// </remarks>
/// </summary>
[ApiController]
[Route("api/statistics")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
public class StatisticsController(
    IUrlStatisticsService statisticsService,
    IUserContext userContext) : ControllerApiBase
{
    // ═════════════════════════════════════════════════════════════════════════
    // PER-URL SLICE ENDPOINTS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     LIGHTWEIGHT — fetched alongside <c>/timeseries</c> on link stats page mount.
    ///
    ///     Returns: core click metrics, time-window counters, top country, top referrer.
    ///     Does NOT return: session engagement metrics → <c>/urls/{id}/engagement</c>
    /// </summary>
    [HttpGet("urls/{shortUrlId:long}/overview", Name = "GetUrlOverview")]
    [ProducesResponseType(typeof(UrlOverview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlOverview(
        long shortUrlId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUrlOverviewAsync(
            shortUrlId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     HEAVY — session-level engagement metrics for a single URL.
    ///     Lazy-loaded when the Engagement panel is first expanded.
    ///
    ///     Why deferred: the sole SQL operation is a full GROUP BY SessionId CTE
    ///     over every ClickEvents row for this URL in the date-range window. For a
    ///     high-traffic link (200 K+ clicks) this is a significant sort + hash
    ///     aggregate that must never execute on the initial page mount.
    ///
    ///     Returns: BounceRate, ReturnVisitorRate, ClicksPerSession,
    ///              AverageSessionDuration, NewVisitors, ReturningVisitors.
    /// </summary>
    [HttpGet("urls/{shortUrlId:long}/engagement", Name = "GetUrlEngagement")]
    [ProducesResponseType(typeof(EngagementMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlEngagement(
        long shortUrlId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUrlEngagementAsync(
            shortUrlId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Daily / weekly click trend. Fetched alongside <c>/overview</c> on page mount.
    /// </summary>
    [HttpGet("urls/{shortUrlId:long}/timeseries", Name = "GetUrlTimeSeries")]
    [ProducesResponseType(typeof(TimeSeriesStats), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlTimeSeries(
        long shortUrlId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUrlTimeSeriesAsync(
            shortUrlId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Country and city breakdown. Lazy-loaded when the Countries tab is first activated.
    /// </summary>
    [HttpGet("urls/{shortUrlId:long}/geography", Name = "GetUrlGeography")]
    [ProducesResponseType(typeof(GeographicalStats), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlGeography(
        long shortUrlId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUrlGeographyAsync(
            shortUrlId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Traffic sources, referrer domains, and UTM campaigns. Lazy-loaded on Traffic tab.
    /// </summary>
    [HttpGet("urls/{shortUrlId:long}/traffic", Name = "GetUrlTraffic")]
    [ProducesResponseType(typeof(TrafficStats), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlTraffic(
        long shortUrlId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUrlTrafficAsync(
            shortUrlId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Device types, browsers, and operating systems. Lazy-loaded on Devices tab.
    /// </summary>
    [HttpGet("urls/{shortUrlId:long}/devices", Name = "GetUrlDevices")]
    [ProducesResponseType(typeof(DeviceStats), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlDevices(
        long shortUrlId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUrlDevicesAsync(
            shortUrlId, startDate, endDate, cancellationToken));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PER-USER (MY-STATS) SLICE ENDPOINTS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     LIGHTWEIGHT — fetched on dashboard mount alongside <c>/my-stats/timeseries</c>.
    ///
    ///     Returns:
    ///     • URL portfolio shape (from ShortUrls — no ClickEvents scan)
    ///     • Click totals + time-window counters (two cheap COUNT_BIG queries)
    ///     • Top country + top referrer (two TOP 1 GROUP BY queries)
    ///
    ///     Does NOT return:
    ///     • Session engagement metrics → <c>/my-stats/engagement</c>
    ///     • Portfolio health / top URLs → <c>/my-stats/top-urls</c>
    /// </summary>
    [HttpGet("my-stats/overview", Name = "GetMyOverview")]
    [ProducesResponseType(typeof(UserOverview), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOverview(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUserOverviewAsync(
            userContext.CurrentUserId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Aggregated daily / weekly trend across all user URLs.
    ///     Fetched on dashboard mount alongside <c>/my-stats/overview</c>.
    /// </summary>
    [HttpGet("my-stats/timeseries", Name = "GetMyTimeSeries")]
    [ProducesResponseType(typeof(TimeSeriesStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTimeSeries(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUserTimeSeriesAsync(
            userContext.CurrentUserId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     HEAVY — session-level engagement metrics. Lazy-loaded when the Engagement
    ///     tab is first activated.
    ///
    ///     Why deferred: the sole SQL operation is a full GROUP BY SessionId CTE over
    ///     the date-range partition. For a user with 500 K clicks this is a multi-second
    ///     sort + hash aggregate that must never block the initial dashboard paint.
    ///
    ///     Returns: BounceRate, ReturnVisitorRate, ClicksPerSession, AverageSessionDuration.
    /// </summary>
    [HttpGet("my-stats/engagement", Name = "GetMyEngagement")]
    [ProducesResponseType(typeof(EngagementMetrics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEngagement(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUserEngagementAsync(
            userContext.CurrentUserId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     HEAVY — portfolio health + top 10 URLs ranked by clicks. Lazy-loaded when
    ///     the Top URLs tab is first activated.
    ///
    ///     Why deferred: requires two expensive operations in one query —
    ///     (1) portfolio health via window functions (MAX OVER, MIN OVER) on a
    ///     per-ShortUrlId GROUP BY aggregate, and (2) a 5-CTE chain that re-scans
    ///     ClickEvents to compute per-URL dominant country, device, and traffic source.
    ///
    ///     Returns: UrlsWithClicks, MostPopularUrl, LeastPopularUrl,
    ///              and the ranked TopUrls list with per-URL signals.
    /// </summary>
    [HttpGet("my-stats/top-urls", Name = "GetMyTopUrls")]
    [ProducesResponseType(typeof(UserTopUrls), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTopUrls(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUserTopUrlsAsync(
            userContext.CurrentUserId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Aggregated geography breakdown (top 5 countries + Other, top 5 cities + Other).
    ///     Lazy-loaded on Geography tab activation.
    /// </summary>
    [HttpGet("my-stats/geography", Name = "GetMyGeography")]
    [ProducesResponseType(typeof(GeographicalStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyGeography(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUserGeographyAsync(
            userContext.CurrentUserId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Aggregated traffic sources, referrers, and UTM campaigns.
    ///     Lazy-loaded on Traffic tab activation.
    /// </summary>
    [HttpGet("my-stats/traffic", Name = "GetMyTraffic")]
    [ProducesResponseType(typeof(TrafficStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTraffic(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUserTrafficAsync(
            userContext.CurrentUserId, startDate, endDate, cancellationToken));
    }

    /// <summary>
    ///     Aggregated device breakdown (top 5 types, top 6 browsers, top 6 OS).
    ///     Lazy-loaded on Devices tab activation.
    /// </summary>
    [HttpGet("my-stats/devices", Name = "GetMyDevices")]
    [ProducesResponseType(typeof(DeviceStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDevices(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        ValidateDateOrder(startDate, endDate);
        return Ok(await statisticsService.GetUserDevicesAsync(
            userContext.CurrentUserId, startDate, endDate, cancellationToken));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ValidateDateOrder(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value >= endDate.Value)
            ValidateDateRange(startDate.Value, endDate.Value); // throws via base
    }
}