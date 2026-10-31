using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Extensions;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Analytics.GetAnalyticsOverview;
using Shortly.Core.Analytics.GetDeviceStats;
using Shortly.Core.Analytics.GetEngagementMetrics;
using Shortly.Core.Analytics.GetGeographicalStats;
using Shortly.Core.Analytics.GetShortUrlPortfolioSummary;
using Shortly.Core.Analytics.GetTimeSeriesStats;
using Shortly.Core.Analytics.GetTopPerformingShortUrls;
using Shortly.Core.Analytics.GetTrafficStats;
using Shortly.Core.Analytics.GetUrlsApproachingClickLimit;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.Abstractions;

namespace Shortly.API.Controllers.Analytics;

[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
[Authorize]
public class AnalyticsController(ISender sender, IUserContext userContext) : ControllerBase
{
    [HttpGet("urls/{shortUrlId:long}/overview", Name = "GetUrlAnalyticsOverview")]
    [ProducesResponseType(typeof(AnalyticsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrlOverview(long shortUrlId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAnalyticsOverviewQuery(shortUrlId, null, null, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("urls/{shortUrlId:long}/engagement", Name = "GetUrlEngagement")]
    [ProducesResponseType(typeof(EngagementMetricsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrlEngagement(long shortUrlId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetEngagementMetricsQuery(shortUrlId, null, null, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("urls/{shortUrlId:long}/geography", Name = "GetUrlGeography")]
    [ProducesResponseType(typeof(GeographicalStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrlGeography(long shortUrlId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetGeographicalStatsQuery(shortUrlId, null, null, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("urls/{shortUrlId:long}/devices", Name = "GetUrlDevices")]
    [ProducesResponseType(typeof(DeviceStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrlDevices(long shortUrlId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDeviceStatsQuery(shortUrlId, null, null, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("urls/{shortUrlId:long}/traffic", Name = "GetUrlTraffic")]
    [ProducesResponseType(typeof(TrafficStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrlTraffic(long shortUrlId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTrafficStatsQuery(shortUrlId, null, null, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("urls/{shortUrlId:long}/timeseries", Name = "GetUrlTimeSeries")]
    [ProducesResponseType(typeof(TimeSeriesStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrlTimeSeries(long shortUrlId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTimeSeriesStatsQuery(shortUrlId, null, null, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/summary", Name = "GetMyPortfolioSummary")]
    [ProducesResponseType(typeof(ShortUrlPortfolioSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySummary(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetShortUrlPortfolioSummaryQuery(userContext.UserId, userContext.OrganizationId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/overview", Name = "GetMyOverview")]
    [ProducesResponseType(typeof(AnalyticsOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOverview([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAnalyticsOverviewQuery(null, userContext.UserId, userContext.OrganizationId, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/engagement", Name = "GetMyEngagement")]
    [ProducesResponseType(typeof(EngagementMetricsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEngagement([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetEngagementMetricsQuery(null, userContext.UserId, userContext.OrganizationId, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/geography", Name = "GetMyGeography")]
    [ProducesResponseType(typeof(GeographicalStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyGeography([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetGeographicalStatsQuery(null, userContext.UserId, userContext.OrganizationId, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/devices", Name = "GetMyDevices")]
    [ProducesResponseType(typeof(DeviceStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDevices([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDeviceStatsQuery(null, userContext.UserId, userContext.OrganizationId, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/traffic", Name = "GetMyTraffic")]
    [ProducesResponseType(typeof(TrafficStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTraffic([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTrafficStatsQuery(null, userContext.UserId, userContext.OrganizationId, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/timeseries", Name = "GetMyTimeSeries")]
    [ProducesResponseType(typeof(TimeSeriesStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTimeSeries([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetTimeSeriesStatsQuery(null, userContext.UserId, userContext.OrganizationId, from, to), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("mine/top-urls", Name = "GetMyTopUrls")]
    [ProducesResponseType(typeof(IReadOnlyList<TopPerformingUrlResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTopUrls([FromQuery] int topCount = 10, [FromQuery] int? timeframeDays = null, CancellationToken cancellationToken = default)
    {
        var timeframe = timeframeDays.HasValue ? TimeSpan.FromDays(timeframeDays.Value) : (TimeSpan?)null;
        var result = await sender.Send(new GetTopPerformingShortUrlsQuery(userContext.UserId, userContext.OrganizationId, topCount, timeframe), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }

    [HttpGet("approaching-limit", Name = "GetUrlsApproachingLimit")]
    [ProducesResponseType(typeof(PagedResponse<ShortUrlResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproachingLimit([FromQuery] double warningThreshold = 0.8, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetUrlsApproachingClickLimitQuery(warningThreshold, page, pageSize), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
    }
}