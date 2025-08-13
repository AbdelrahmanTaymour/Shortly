using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.ServiceContracts.UserManagement;

namespace Shortly.API.Controllers;

/// <summary>
///     Provides endpoints for managing and monitoring user usage statistics,
///     including tracking link and QR code creation, checking usage limits,
///     retrieving usage reports, and performing monthly resets.
/// </summary>
[ApiController]
[Route("api/usage")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class UsageController(IUserUsageService usageService) : ControllerApiBase
{
    /// <summary>
    ///     Retrieves comprehensive usage statistics for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose usage statistics are to be retrieved.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     A <see cref="UserUsageDto" /> containing the user's link and QR code usage statistics.
    /// </returns>
    /// <response code="200">Returns the usage statistics for the specified user.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view usage statistics.</response>
    /// <response code="404">No usage record was found for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/Usage/{userId}
    /// </example>
    [HttpGet("{userId:guid:required}", Name = "GetUserUsageStats")]
    [ProducesResponseType(typeof(UserUsageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ViewUserUsage)]
    public async Task<IActionResult> GetUserUsageStats(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await usageService.GetUsageStatsAsync(userId, cancellationToken);
        return Ok(result);
    }


    /// <summary>
    ///     Tracks the creation of a new link for a specific user by incrementing their monthly link count.
    /// </summary>
    /// <param name="userId">The unique identifier of the user creating the link.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the link creation was successfully tracked; otherwise, <c>false</c>.</returns>
    /// <response code="200">Link creation was tracked successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to track link creation.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     POST /api/Usage/{userId}/track-link
    /// </example>
    /// <remarks></remarks>
    [HttpPost("{userId:guid:required}/track-link", Name = "TrackLinkCreation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UserUsageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserUsage)]
    public async Task<IActionResult> TrackLinkCreation(Guid userId, CancellationToken cancellationToken = default)
    {
        var success = await usageService.TrackLinkCreationAsync(userId, cancellationToken);
        return Ok(success);
    }


    /// <summary>
    ///     Tracks the creation of a new QR code for a specific user by incrementing their monthly QR code count.
    /// </summary>
    /// <param name="userId">The unique identifier of the user creating the QR code.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the QR code creation was successfully tracked; otherwise, <c>false</c>.</returns>
    /// <response code="200">QR code creation was tracked successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to track QR code creation.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     POST /api/UserUsage/{userId}/track-qr
    /// </example>
    [HttpPost("{userId:guid}/track-qr", Name = "TrackQrCodeCreation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserUsage)]
    public async Task<IActionResult> TrackQrCodeCreation(Guid userId, CancellationToken cancellationToken)
    {
        var success = await usageService.TrackQrCodeCreationAsync(userId, cancellationToken);
        return Ok(success);
    }


    /// <summary>
    ///     Checks if the user can create more links this month based on their subscription plan limits.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the user can create more links; otherwise, <c>false</c>.</returns>
    /// <response code="200">Returns whether the user can create more links.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view link limits.</response>
    /// <response code="404">No usage record was found for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/Usage/{userId}/can-create-links
    /// </example>
    [HttpGet("{userId:guid:required}/can-create-links", Name = "CanCreateMoreLinks")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ViewUserUsage)]
    public async Task<IActionResult> CanCreateMoreLinks(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await usageService.CanCreateMoreLinksAsync(userId, cancellationToken);
        return Ok(result);
    }


    /// <summary>
    ///     Checks if the user can create more QR codes this month based on their subscription plan limits.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the user can create more QR codes; otherwise, <c>false</c>.</returns>
    /// <response code="200">Returns whether the user can create more QR codes.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view QR code limits.</response>
    /// <response code="404">No usage record was found for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/Usage/{userId}/can-create-qr
    /// </example>
    [HttpGet("{userId:guid}/can-create-qr", Name = "CanCreateMoreQrCodes")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ViewUserUsage)]
    public async Task<IActionResult> CanCreateMoreQrCodes(Guid userId, CancellationToken cancellationToken)
    {
        var canCreate = await usageService.CanCreateMoreQrCodesAsync(userId, cancellationToken);
        return Ok(canCreate);
    }

    
    /// <summary>
    ///     Gets the remaining number of links the user can create this month.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The number of remaining links for the current month.</returns>
    /// <response code="200">Returns the remaining link count.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view link limits.</response>
    /// <response code="404">No usage record was found for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/Usage/{userId}/remaining-links
    /// </example>
    [HttpGet("{userId:guid}/remaining-links", Name = "GetRemainingLinks")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ViewUserUsage)]
    public async Task<IActionResult> GetRemainingLinks(Guid userId, CancellationToken cancellationToken)
    {
        var remaining = await usageService.GetRemainingLinksAsync(userId, cancellationToken);
        return Ok(remaining);
    }

    
    /// <summary>
    ///     Gets the remaining number of QR codes the user can create this month.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The number of remaining QR codes for the current month.</returns>
    /// <response code="200">Returns the remaining QR code count.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to view QR code limits.</response>
    /// <response code="404">No usage record was found for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/Usage/{userId}/remaining-qr
    /// </example>
    [HttpGet("{userId:guid}/remaining-qr", Name = "GetRemainingQrCodes")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ViewUserUsage)]
    public async Task<IActionResult> GetRemainingQrCodes(Guid userId, CancellationToken cancellationToken)
    {
        var remaining = await usageService.GetRemainingQrCodesAsync(userId, cancellationToken);
        return Ok(remaining);
    }


    /// <summary>
    ///     Checks whether the user has exceeded their monthly limits for links or QR codes.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the user has exceeded their limits; otherwise, <c>false</c>.</returns>
    /// <response code="200">Returns whether the limits have been exceeded.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to check links or QR limits.</response>
    /// <response code="404">No usage record was found for the specified user.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/Usage/{userId}/has-exceeded
    /// </example>
    [HttpGet("{userId:guid}/has-exceeded", Name = "HasExceededLimits")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ViewUserUsage)]
    public async Task<IActionResult> HasExceededLimits(Guid userId, CancellationToken cancellationToken)
    {
        var exceeded = await usageService.HasExceededLimitsAsync(userId, cancellationToken);
        return Ok(exceeded);
    }

    
    /// <summary>
    ///     Resets the monthly usage statistics (links and QR codes) for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose usage will be reset.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if the reset was successful; otherwise, <c>false</c>.</returns>
    /// <response code="200">Reset completed successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to reset monthly usage.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     PUT /api/Usage/{userId}/reset
    /// </example>
    [HttpPut("{userId:guid}/reset", Name = "ResetMonthlyUsage")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserUsage)]
    public async Task<IActionResult> ResetMonthlyUsage(Guid userId, CancellationToken cancellationToken)
    {
        var success = await usageService.ResetMonthlyUsageAsync(userId, cancellationToken);
        return Ok(success);
    }

    
    /// <summary>
    ///     Resets monthly usage statistics for all eligible users.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><c>true</c> if at least one user's usage was reset; otherwise, <c>false</c>.</returns>
    /// <response code="200">Reset completed successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to reset the monthly usage of all users.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     PUT /api/Usage/reset-all
    /// </example>
    [HttpPut("reset-all", Name = "ResetMonthlyUsageForAll")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserUsage)]
    public async Task<IActionResult> ResetMonthlyUsageForAll(CancellationToken cancellationToken)
    {
        var success = await usageService.ResetMonthlyUsageForAllAsync(cancellationToken);
        return Ok(success);
    }

    
    /// <summary>
    ///     Generates a usage report for all users whose reset date falls within the specified date range.
    /// </summary>
    /// <param name="from">The start date of the reporting period (inclusive).</param>
    /// <param name="to">The end date of the reporting period (inclusive).</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    ///     A collection of <see cref="UserUsageDto" /> objects representing usage data for the period.
    /// </returns>
    /// <response code="200">Report generated successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to usage reports.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/Usage/report?from=2025-01-01&amp;to=2025-01-31
    /// </example>
    [HttpGet("report", Name = "GetUsageReport")]
    [ProducesResponseType(typeof(IEnumerable<UserUsageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ViewUserUsage)]
    public async Task<IActionResult> GetUsageReport(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var report = await usageService.GetUsageReportAsync(from, to, cancellationToken);
        return Ok(report);
    }
}