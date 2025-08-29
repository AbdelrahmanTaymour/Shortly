using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

/// <summary>
/// API controller for managing organization usage statistics and tracking resource consumption.
/// Provides endpoints for retrieving usage stats, tracking resource creation, checking limits, and resetting monthly usage.
/// </summary>
[ApiController]
[Route("api/organization-usage")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[RequirePermission(enPermissions.ManageOrgUsage)]
public class OrganizationUsageController(IOrganizationUsageService organizationUsageService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves usage statistics for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique ID of the organization.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The organization's usage statistics.</returns>
    /// <example>GET /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/stats</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/stats
    /// </remarks>
    /// <response code="200">Returns the organization usage statistics.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="404">If the organization usage record is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{organizationId:guid}/stats", Name = "GetOrganizationUsageStats")]
    [ProducesResponseType(typeof(OrganizationUsageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsageStats(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var usageStats = await organizationUsageService.GetUsageStatsAsync(organizationId, cancellationToken);
        return Ok(usageStats);
    }

    /// <summary>
    /// Tracks a link creation event for the specified organization.
    /// </summary>
    /// <param name="organizationId">The unique ID of the organization.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A boolean indicating whether the tracking was successful.</returns>
    /// <example>POST /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/track/link</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/track/link
    /// </remarks>
    /// <response code="200">Returns true if link creation was successfully tracked.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{organizationId:guid}/track/link", Name = "TrackOrganizationLinkCreation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrackLinkCreation(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await organizationUsageService.TrackLinkCreationAsync(organizationId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tracks a QR code creation event for the specified organization.
    /// </summary>
    /// <param name="organizationId">The unique ID of the organization.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A boolean indicating whether the tracking was successful.</returns>
    /// <example>POST /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/track/qrcode</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/track/qrcode
    /// </remarks>
    /// <response code="200">Returns true if QR code creation was successfully tracked.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{organizationId:guid}/track/qrcode", Name = "TrackOrganizationQrCodeCreation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TrackQrCodeCreation(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await organizationUsageService.TrackQrCodeCreationAsync(organizationId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Checks if the organization can create more links within their monthly limit.
    /// </summary>
    /// <param name="organizationId">The unique ID of the organization.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A boolean indicating whether the organization can create more links.</returns>
    /// <example>GET /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/can-create-links</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/can-create-links
    /// </remarks>
    /// <response code="200">Returns true if the organization can create more links.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{organizationId:guid}/can-create-links", Name = "CanOrganizationCreateMoreLinks")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CanCreateMoreLinks(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var canCreate = await organizationUsageService.CanCreateMoreLinksAsync(organizationId, cancellationToken);
        return Ok(canCreate);
    }

    /// <summary>
    /// Checks if the organization can create more QR codes within their monthly limit.
    /// </summary>
    /// <param name="organizationId">The unique ID of the organization.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A boolean indicating whether the organization can create more QR codes.</returns>
    /// <example>GET /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/can-create-qrcodes</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/can-create-qrcodes
    /// </remarks>
    /// <response code="200">Returns true if the organization can create more QR codes.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{organizationId:guid}/can-create-qrcodes", Name = "CanOrganizationCreateMoreQrCodes")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CanCreateMoreQrCodes(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var canCreate = await organizationUsageService.CanCreateMoreQrCodesAsync(organizationId, cancellationToken);
        return Ok(canCreate);
    }

    /// <summary>
    /// Resets the monthly usage statistics for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique ID of the organization.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A boolean indicating whether the reset was successful.</returns>
    /// <example>POST /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/reset-usage</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-usage/550e8400-e29b-41d4-a716-446655440000/reset-usage
    /// </remarks>
    /// <response code="200">Returns true if the monthly usage was successfully reset.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{organizationId:guid}/reset-usage", Name = "ResetOrganizationMonthlyUsage")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetMonthlyUsage(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var result = await organizationUsageService.ResetMonthlyUsageAsync(organizationId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Resets the monthly usage statistics for all organizations.
    /// This is typically used for scheduled monthly maintenance tasks.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of organizations that had their usage reset.</returns>
    /// <example>POST /api/organization-usage/reset-all-usage</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-usage/reset-all-usage
    /// </remarks>
    /// <response code="200">Returns the count of organizations that had their usage reset.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read teams.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("reset-all-usage", Name = "ResetAllOrganizationsMonthlyUsage")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetMonthlyUsageForAll(CancellationToken cancellationToken = default)
    {
        var resetCount = await organizationUsageService.ResetMonthlyUsageForAllAsync(cancellationToken);
        return Ok(resetCount);
    }
}