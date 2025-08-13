using Microsoft.AspNetCore.Mvc;
using Shortly.API.Authorization;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Enums;

namespace Shortly.API.Controllers;

/// <summary>
///     Manages user profile operations including viewing, updating, quota monitoring, and account deletion requests.
/// </summary>
/// <remarks>
///     This controller provides endpoints for authenticated users to manage their own profiles.
///     All operations are subject to permission-based authorization and operate only on the current user's data.
/// </remarks>
[ApiController]
[Route("api/profile")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ProfileController(IUserProfileService profileService) : ControllerApiBase
{
    /// <summary>
    ///     Retrieves the current user's profile information.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     Returns the user's profile data including personal information, preferences, and settings.
    /// </returns>
    /// <response code="200">Successfully retrieved the user profile.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User lacks permission to view their own profile.</response>
    /// <response code="404">User profile not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/profile
    /// </example>
    [HttpGet(Name = "GetProfile")]
    [RequirePermission(enPermissions.ViewOwnProfile)]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUserId();
        var profile = await profileService.GetProfileAsync(currentUser, cancellationToken);
        return Ok(profile);
    }


    /// <summary>
    ///     Retrieves the current user's monthly quota status and usage statistics.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     Returns quota information including current usage, limits, and remaining allowances for the current month.
    /// </returns>
    /// <response code="200">Successfully retrieved the quota status.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User lacks permission to view usage statistics.</response>
    /// <response code="404">Quota information isn't found for the user.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/profile/quota-status
    /// </example>
    [HttpGet("quota-status", Name = "GetMonthlyQuotaStatus")]
    [RequirePermission(enPermissions.ViewOwnUsageStats)]
    [ProducesResponseType(typeof(QuotaStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMonthlyQuotaStatus(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUserId();
        var quotaStatus = await profileService.GetMonthlyQuotaStatusAsync(currentUser, cancellationToken);
        return Ok(quotaStatus);
    }

    /// <summary>
    ///     Updates the current user's profile information.
    /// </summary>
    /// <param name="request">The profile update request containing the fields to be modified.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     Returns a success indicator and any validation messages.
    /// </returns>
    /// <response code="200">Profile was successfully updated.</response>
    /// <response code="400">The request contains invalid data or validation errors.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User lacks permission to update their profile.</response>
    /// <response code="404">User profile not found.</response>
    /// <response code="409">A conflict occurred (e.g., email already exists).</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     PUT /api/profile/update
    /// </example>
    [HttpPut("update", Name = "UpdateProfile")]
    [RequirePermission(enPermissions.UpdateOwnProfile)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateProfile(UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUserId();
        var succeed = await profileService.UpdateProfileAsync(currentUser, request, cancellationToken);
        return Ok(succeed);
    }

    
    
    /// <summary>
    ///     Performs a soft delete on a user, marking them as deleted without permanently removing the record.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    ///     Returns confirmation that the deletion request has been processed.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The user will be immediately logged out from all devices and applications, and will not be able to
    ///         authenticate using any previously issued tokens.
    ///     </para>
    /// </remarks>
    /// <response code="200">Account deletion request was successfully submitted.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User lacks permission to delete their account.</response>
    /// <response code="404">User account not found.</response>
    /// <response code="409">Account deletion request already pending or an account cannot be deleted.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     DELETE /api/profile
    /// </example>
    [HttpDelete(Name = "RequestAccountDeletion")]
    [RequirePermission(enPermissions.DeleteOwnAccount)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestAccountDeletion(CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUserId();
        var succeed = await profileService.RequestAccountDeletionAsync(currentUser, cancellationToken);
        return Ok(succeed);
    }
}