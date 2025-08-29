using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/short-urls/analytics")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ShortUrlAnalyticsController(IShortUrlAnalyticsService analyticsService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves the total count of short URLs in the system.
    /// </summary>
    /// <param name="activeOnly">Optional. If true, returns only active URLs. If false or not provided, returns all URLs including inactive ones. Default is false.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The total count of short URLs.</returns>
    /// <example>GET /api/short-urls/analytics/total-count?activeOnly=true</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/analytics/total-count
    ///     GET /api/short-urls/analytics/total-count?activeOnly=true
    ///     GET /api/short-urls/analytics/total-count?activeOnly=false
    ///
    /// This endpoint provides system-wide statistics for monitoring and dashboard purposes.
    /// When activeOnly is true, it excludes deactivated, expired, and deleted URLs from the count.
    /// Useful for generating reports and understanding system usage patterns.
    /// </remarks>
    /// <response code="200">Returns the total count of short URLs.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view system analytics.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("total-count", Name = "GetTotalUrlCount")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ViewUrlsAnalytics)]
    public async Task<IActionResult> GetTotalCount([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var totalCount = await analyticsService.GetTotalCountAsync(activeOnly, cancellationToken);
        return Ok(totalCount);
    }

    /// <summary>
    /// Retrieves the total number of clicks for a specific short URL.
    /// </summary>
    /// <param name="shortUrlId">The unique identifier of the short URL to get click statistics for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The total number of clicks for the specified short URL.</returns>
    /// <example>GET /api/short-urls/analytics/123456789/total-clicks</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/analytics/123456789/total-clicks
    /// 
    /// This endpoint provides click analytics for individual short URLs.
    /// The click count includes all successful redirections and excludes failed attempts or blocked access.
    /// Useful for monitoring individual URL performance and engagement metrics.
    /// </remarks>
    /// <response code="200">Returns the total click count for the short URL.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view analytics for this URL.</response>
    /// <response code="404">Short URL with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{shortUrlId:long}/total-clicks", Name = "GetTotalClicksForUrl")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOwnAnalytics | enPermissions.ReadTeamAnalytics | enPermissions.ReadOrgAnalytics)]
    public async Task<IActionResult> GetTotalClicks(long shortUrlId, CancellationToken cancellationToken = default)
    {
        var totalClicks = await analyticsService.GetTotalClicksAsync(shortUrlId, cancellationToken);
        return Ok(totalClicks);
    }

    /// <summary>
    /// Retrieves the most popular short URLs based on click count, optionally filtered by timeframe and user.
    /// </summary>
    /// <param name="topCount">The number of top URLs to retrieve. Default is 10, maximum is 100.</param>
    /// <param name="timeframeDays">Optional. Filter results to URLs clicked within the last specified number of days. If not provided, considers all-time data.</param>
    /// <param name="userId">Optional. Filter results to URLs created by the specified user. If not provided, includes URLs from all users.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of the most popular short URLs ordered by click count (descending).</returns>
    /// <example>GET /api/short-urls/analytics/most-popular?topCount=5&amp;timeframeDays=30&amp;userId=12345678-1234-1234-1234-123456789012</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/analytics/most-popular
    ///     GET /api/short-urls/analytics/most-popular?topCount=5
    ///     GET /api/short-urls/analytics/most-popular?topCount=20&amp;timeframeDays=30
    ///     GET /api/short-urls/analytics/most-popular?userId=12345678-1234-1234-1234-123456789012
    ///     GET /api/short-urls/analytics/most-popular?topCount=5&amp;timeframeDays=7&amp;userId=12345678-1234-1234-1234-123456789012
    /// 
    /// This endpoint provides insights into the most engaging content by analyzing click patterns.
    /// Results are ordered by total clicks in descending order (most popular first).
    /// The timeframeDays parameter allows for trend analysis over specific periods.
    /// Useful for content performance analysis, trending content identification, and user engagement metrics.
    /// </remarks>
    /// <response code="200">Returns the list of most popular short URLs.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid topCount range, negative timeframeDays, invalid userId format).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view popularity analytics.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("most-popular", Name = "GetMostPopularUrls")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOwnAnalytics | enPermissions.ReadTeamAnalytics | enPermissions.ReadOrgAnalytics)]
    public async Task<IActionResult> GetMostPopular([FromQuery] int topCount = 10, [FromQuery] int? timeframeDays = null, [FromQuery] Guid? userId = null, CancellationToken cancellationToken = default)
    {
        if (topCount < 1 || topCount > 100)
            return BadRequest("topCount must be between 1 and 100.");

        if (timeframeDays is < 0)
            return BadRequest("timeframeDays must be a non-negative value.");

        TimeSpan? timeframe = timeframeDays.HasValue ? TimeSpan.FromDays(timeframeDays.Value) : null;
        var popularUrls = await analyticsService.GetMostPopularUrlAsync(topCount, timeframe, userId, cancellationToken);
        return Ok(popularUrls);
    }

    /// <summary>
    /// Retrieves a comprehensive analytics summary for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to get analytics for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A comprehensive analytics summary including URL count, total clicks, and other user-specific metrics.</returns>
    /// <example>GET /api/short-urls/analytics/user/12345678-1234-1234-1234-123456789012/summary</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/analytics/user/12345678-1234-1234-1234-123456789012/summary
    /// 
    /// This endpoint provides a comprehensive overview of user activity and engagement metrics.
    /// The summary typically includes metrics such as:
    /// - Total number of URLs created by the user
    /// - Total clicks across all user's URLs
    /// - Average clicks per URL
    /// - Most popular URLs
    /// - Recent activity trends
    /// 
    /// Useful for user dashboards, performance reports, and individual user analytics.
    /// </remarks>
    /// <response code="200">Returns the user analytics summary.</response>
    /// <response code="400">The userId parameter is invalid (e.g., invalid GUID format).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view analytics for the specified user.</response>
    /// <response code="404">User with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("user/{userId:guid}/summary", Name = "GetUserAnalyticsSummary")]
    [ProducesResponseType(typeof(UserAnalyticsSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOwnAnalytics)]
    public async Task<IActionResult> GetUserAnalytics(Guid userId, CancellationToken cancellationToken = default)
    {
        var userAnalytics = await analyticsService.GetUserAnalyticsAsync(userId, cancellationToken);
        return Ok(userAnalytics);
    }

    /// <summary>
    /// Retrieves comprehensive analytics summary for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to get analytics for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A comprehensive analytics summary including organization-wide URL metrics and team performance data.</returns>
    /// <example>GET /api/short-urls/analytics/organization/87654321-4321-4321-4321-210987654321/summary</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/analytics/organization/87654321-4321-4321-4321-210987654321/summary
    /// 
    /// This endpoint provides a comprehensive overview of organization-wide activity and engagement metrics.
    /// The summary typically includes metrics such as:
    /// - Total number of URLs created by organization members
    /// - Total clicks across all organization URLs
    /// - Top performing members and their contribution
    /// - Most popular organization URLs
    /// - Usage trends and patterns
    /// - Team performance metrics
    /// 
    /// Useful for organization dashboards, team performance analysis, and administrative reporting.
    /// Only users with appropriate organization-level permissions can access this data.
    /// </remarks>
    /// <response code="200">Returns the organization analytics summary.</response>
    /// <response code="400">The organizationId parameter is invalid (e.g., invalid GUID format).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view analytics for the specified organization.</response>
    /// <response code="404">Organization with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("organization/{organizationId:guid}/summary", Name = "GetOrganizationAnalyticsSummary")]
    [ProducesResponseType(typeof(OrganizationAnalyticsSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOrgAnalytics)]
    public async Task<IActionResult> GetOrganizationAnalytics(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var orgAnalytics = await analyticsService.GetOrganizationAnalyticsAsync(organizationId, cancellationToken);
        return Ok(orgAnalytics);
    }

    /// <summary>
    /// Retrieves short URLs that are approaching their click limits with pagination support.
    /// </summary>
    /// <param name="warningThreshold">The threshold percentage (as decimal) at which URLs should be considered approaching their limit. Default is 0.8 (80%). Must be between 0.1 and 1.0.</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of short URLs that are approaching their click limits.</returns>
    /// <example>GET /api/short-urls/analytics/approaching-limit?warningThreshold=0.9&amp;pageNumber=1&amp;pageSize=25</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/analytics/approaching-limit
    ///     GET /api/short-urls/analytics/approaching-limit?warningThreshold=0.9
    ///     GET /api/short-urls/analytics/approaching-limit?warningThreshold=0.85&amp;pageNumber=1&amp;pageSize=25
    /// 
    /// This endpoint identifies URLs that are close to reaching their configured click limits.
    /// For example, if a URL has a click limit of 100 and warningThreshold is 0.8, it will be included
    /// when it reaches 80 or more clicks.
    /// 
    /// Useful for:
    /// - Proactive monitoring and alerting
    /// - Preventing service disruptions due to exceeded limits  
    /// - Capacity planning and limit management
    /// - Automated notifications to URL owners
    /// 
    /// Only URLs with defined click limits are considered in this analysis.
    /// </remarks>
    /// <response code="200">Returns the paginated list of URLs approaching their click limits.</response>
    /// <response code="400">The request parameters are invalid (e.g., warningThreshold out of range, invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view limit analytics.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("approaching-limit", Name = "GetUrlsApproachingLimit")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOwnAnalytics | enPermissions.ReadTeamAnalytics | enPermissions.ReadOrgAnalytics)]
    public async Task<IActionResult> GetApproachingLimit([FromQuery] double warningThreshold = 0.8, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (warningThreshold < 0.1 || warningThreshold > 1.0)
            return BadRequest("warningThreshold must be between 0.1 and 1.0.");

        if (pageNumber < 1)
            return BadRequest("Page number must be greater than 0.");
        
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100.");

        var approachingLimitUrls = await analyticsService.GetApproachingLimitAsync(warningThreshold, pageNumber, pageSize, cancellationToken);
        return Ok(approachingLimitUrls);
    }
}