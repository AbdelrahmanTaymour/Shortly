using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/short-urls/query")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ShortUrlQueryController(IShortUrlQueryService queryService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves short URLs created by a specific user with pagination support.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose URLs to retrieve.</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of short URLs created by the specified user.</returns>
    /// <example>GET /api/short-urls/query/user/12345678-1234-1234-1234-123456789012?pageNumber=1&amp;pageSize=20</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/query/user/12345678-1234-1234-1234-123456789012?pageNumber=1&amp;pageSize=20
    /// 
    /// This endpoint returns all short URLs created by the specified user, including both public and private URLs
    /// that the authenticated user has permission to view.
    /// </remarks>
    /// <response code="200">Returns the paginated list of user's short URLs.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid userId format, invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view URLs for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("user/{userId:guid:required}", Name = "GetShortUrlsByUserId")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOwnLinks)]
    public async Task<IActionResult> GetByUserId(Guid userId, [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ValidatePage(pageNumber, pageSize);
        var urls = await queryService.GetByUserIdAsync(userId, pageNumber, pageSize, cancellationToken);
        return Ok(urls);
    }
    
    
    /// <summary>
    /// Retrieves short URLs created by users within a specific organization with pagination support.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization whose URLs to retrieve.</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of short URLs created by organization members.</returns>
    /// <example>GET /api/short-urls/query/organization/87654321-4321-4321-4321-210987654321?pageNumber=1&amp;pageSize=25</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/query/organization/87654321-4321-4321-4321-210987654321?pageNumber=1&amp;pageSize=25
    /// 
    /// This endpoint returns all short URLs created by members of the specified organization.
    /// Only users with appropriate permissions can access organization-level data.
    /// </remarks>
    /// <response code="200">Returns the paginated list of organization's short URLs.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid organizationId format, invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view URLs for the specified organization.</response>
    /// <response code="404">Organization with the specified ID was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("organization/{organizationId:guid}", Name = "GetShortUrlsByOrganizationId")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadOrgLinks)]
    public async Task<IActionResult> GetByOrganizationId(Guid organizationId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ValidatePage(pageNumber, pageSize);
        var urls = await queryService.GetByOrganizationIdAsync(organizationId, pageNumber, pageSize, cancellationToken);
        return Ok(urls);
    }

    
      /// <summary>
    /// Retrieves anonymous short URLs created within a specific date range with pagination support.
    /// </summary>
    /// <param name="startDate">The start date for the date range filter (inclusive).</param>
    /// <param name="endDate">The end date for the date range filter (inclusive).</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of anonymous short URLs created within the specified date range.</returns>
    /// <example>GET /api/short-urls/query/anonymous?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z&amp;pageNumber=1&amp;pageSize=30</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/query/anonymous?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z&amp;pageNumber=1&amp;pageSize=30
    /// 
    /// This endpoint returns short URLs that were created by anonymous users (not logged in) within the specified date range.
    /// Useful for analytics and monitoring anonymous usage patterns.
    /// </remarks>
    /// <response code="200">Returns the paginated list of anonymous short URLs within the date range.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid date format, start date after end date, invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view anonymous URLs.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("anonymous", Name = "GetAnonymousShortUrlsByDateRange")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadUrls)]
    public async Task<IActionResult> GetAnonymousUrlsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ValidatePage(pageNumber, pageSize);
        ValidateDateRange(startDate, endDate);
        
        var urls = await queryService.GetAnonymousUrlsByDateRangeAsync(startDate, endDate, pageNumber, pageSize, cancellationToken);
        return Ok(urls);
    }

    /// <summary>
    /// Retrieves expired short URLs with pagination support.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of expired short URLs.</returns>
    /// <example>GET /api/short-urls/query/expired?pageNumber=1&amp;pageSize=20</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/query/expired?pageNumber=1&amp;pageSize=20
    /// 
    /// This endpoint returns all short URLs that have passed their expiration date and are no longer active.
    /// Useful for cleanup operations and expired URL management.
    /// </remarks>
    /// <response code="200">Returns the paginated list of expired short URLs.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view expired URLs.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("expired", Name = "GetExpiredShortUrls")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadUrls | enPermissions.ReadOrgLinks | enPermissions.ReadOwnLinks)]
    public async Task<IActionResult> GetExpired([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ValidatePage(pageNumber, pageSize);
        var nowUtc = DateTime.UtcNow;
        var urls = await queryService.GetExpiredAsync(nowUtc, pageNumber, pageSize, cancellationToken);
        return Ok(urls);
    }

    /// <summary>
    /// Retrieves private short URLs for a specific user with pagination support.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose private URLs to retrieve.</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, the maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of private short URLs for the specified user.</returns>
    /// <example>GET /api/short-urls/query/private/12345678-1234-1234-1234-123456789012?pageNumber=1&amp;pageSize=15</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/query/private/12345678-1234-1234-1234-123456789012?pageNumber=1&amp;pageSize=15
    /// 
    /// This endpoint returns only private short URLs created by the specified user.
    /// Private URLs are not visible to other users and require special permissions to access.
    /// </remarks>
    /// <response code="200">Returns the paginated list of user's private short URLs.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid userId format, invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view private URLs for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("private/{userId:guid}", Name = "GetPrivateShortUrlsByUserId")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadUrls | enPermissions.ReadOrgLinks | enPermissions.ReadOwnLinks)]
    public async Task<IActionResult> GetPrivateLinks(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ValidatePage(pageNumber, pageSize);

        var urls = await queryService.GetPrivateLinksAsync(userId, pageNumber, pageSize, cancellationToken);
        return Ok(urls);
    }

    /// <summary>
    /// Retrieves short URLs created within a specific date range with pagination support.
    /// </summary>
    /// <param name="startDate">The start date for the date range filter (inclusive).</param>
    /// <param name="endDate">The end date for the date range filter (inclusive).</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of short URLs created within the specified date range.</returns>
    /// <example>GET /api/short-urls/query/date-range?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z&amp;pageNumber=1&amp;pageSize=40</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///     GET /api/short-urls/query/date-range?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z&amp;pageNumber=1&amp;pageSize=40
    /// 
    /// This endpoint returns all short URLs (both anonymous and authenticated users) created within the specified date range.
    /// Useful for analytics, reporting, and monitoring URL creation patterns over time.
    /// </remarks>
    /// <response code="200">Returns the paginated list of short URLs within the date range.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid date format, start date after end date, invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view URLs within the date range.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("date-range", Name = "GetShortUrlsByDateRange")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadUrls)]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
       ValidatePage(pageNumber, pageSize);
       ValidateDateRange(startDate, endDate);

        var urls = await queryService.GetByDateRangeAsync(startDate, endDate, pageNumber, pageSize, cancellationToken);
        return Ok(urls);
    }

    /// <summary>
    /// Retrieves duplicate short URLs grouped by original URL.
    /// </summary>
    /// <param name="userId">Optional. The unique identifier of the user to filter duplicates for. If not provided, searches across all users.</param>
    /// <param name="organizationId">Optional. The unique identifier of the organization to filter duplicates for. If not provided, search across all organizations.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A list of duplicate URL groups, where each group contains URLs that point to the same original URL.</returns>
    /// <example>GET /api/short-urls/query/duplicates?userId=12345678-1234-1234-1234-123456789012</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/query/duplicates
    ///     GET /api/short-urls/query/duplicates?userId=12345678-1234-1234-1234-123456789012
    ///     GET /api/short-urls/query/duplicates?organizationId=87654321-4321-4321-4321-210987654321
    ///     GET /api/short-urls/query/duplicates?userId=12345678-1234-1234-1234-123456789012&amp;organizationId=87654321-4321-4321-4321-210987654321
    /// 
    /// This endpoint identifies and returns groups of short URLs that point to the same original URL.
    /// Useful for cleanup operations, optimization, and identifying redundant short URLs.
    /// If neither userId nor organizationId is provided, it searches for duplicates across the entire system (admin-level operation).
    /// </remarks>
    /// <response code="200">Returns the list of duplicate URL groups.</response>
    /// <response code="400">The request parameters are invalid (e.g., invalid GUID formats).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view duplicates for the specified scope.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("duplicates", Name = "GetDuplicateShortUrls")]
    [ProducesResponseType(typeof(IEnumerable<DuplicatesUrlsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadUrls | enPermissions.ReadOrgLinks | enPermissions.ReadOwnLinks)]
    public async Task<IActionResult> GetDuplicateUrls([FromQuery] Guid? userId = null, [FromQuery] Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var duplicates = await queryService.GetDuplicateUrlsAsync(userId, organizationId, cancellationToken);
        return Ok(duplicates);
    }

    /// <summary>
    /// Retrieves unused short URLs (URLs that have never been clicked) with pagination support.
    /// </summary>
    /// <param name="olderThanDays">Optional. Filter to include only URLs that are older than the specified number of days. If not provided, returns all unused URLs regardless of age.</param>
    /// <param name="pageNumber">The page number to retrieve (starting from 1). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, maximum is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of unused short URLs.</returns>
    /// <example>GET /api/short-urls/query/unused?olderThanDays=30&amp;pageNumber=1&amp;pageSize=25</example>
    /// <remarks>
    /// Sample Requests:
    ///
    ///     GET /api/short-urls/query/unused
    ///     GET /api/short-urls/query/unused?olderThanDays=30&amp;pageNumber=1&amp;pageSize=25
    ///     GET /api/short-urls/query/unused?pageNumber=2&amp;pageSize=50
    /// 
    /// This endpoint returns short URLs that have never been accessed (click count is 0).
    /// The olderThanDays parameter helps identify URLs that have been unused for a specific period,
    /// which is useful for cleanup operations and identifying potentially obsolete URLs.
    /// If olderThanDays is not specified, all unused URLs are returned regardless of their creation date.
    /// </remarks>
    /// <response code="200">Returns the paginated list of unused short URLs.</response>
    /// <response code="400">The request parameters are invalid (e.g., negative olderThanDays value, invalid pagination parameters).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view unused URLs.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("unused", Name = "GetUnusedShortUrls")]
    [ProducesResponseType(typeof(IEnumerable<ShortUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [RequirePermission(enPermissions.ReadUrls | enPermissions.ReadOrgLinks | enPermissions.ReadOwnLinks)]
    public async Task<IActionResult> GetUnusedUrls([FromQuery] int? olderThanDays = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (olderThanDays is < 0)
            return BadRequest("olderThanDays must be a non-negative value.");
        
        ValidatePage(pageNumber, pageSize);

        TimeSpan? olderThan = olderThanDays.HasValue ? TimeSpan.FromDays(olderThanDays.Value) : null;
        var urls = await queryService.GetUnusedUrlsAsync(olderThan, pageNumber, pageSize, cancellationToken);
        return Ok(urls);
    }
    
}