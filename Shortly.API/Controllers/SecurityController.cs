using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.ServiceContracts.UserManagement;

namespace Shortly.API.Controllers;

/// <summary>
///     Manages user account security operations, including login failure tracking, account locking, and security
///     monitoring.
/// </summary>
/// <remarks>
///     This controller provides endpoints for administrators and security systems to manage user account security.
///     Operations include recording failed login attempts, locking or unlocking accounts, and checking lock status.
///     All operations require appropriate administrative permissions.
/// </remarks>
[ApiController]
[Route("api/users/{userId:guid:required}/security")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class SecurityController(IUserSecurityService securityService) : ControllerApiBase
{

    /// <summary>
    ///     Retrieves the current security status of the specified user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve security information for.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    /// <returns>
    ///     A <see cref="UserSecurityStatusResponse"/> containing the user's lock status, number of failed login attempts,
    ///     reason for lockout (if any), lock expiration date, and estimated days remaining until unlocking.
    /// </returns>
    /// <remarks>
    ///     This endpoint is used by administrators or internal systems to assess the current security state of a user account.
    ///     It provides information such as whether the account is locked, the number of failed login attempts,
    ///     the reason for lockout, and how long it remains until the account is automatically unlocked (if applicable).
    /// </remarks>
    /// <response code="200">The user's security status was retrieved successfully.</response>
    /// <response code="400">The request is invalid due to incorrect user ID format or parameters.</response>
    /// <response code="404">No user or security record was found for the specified ID.</response>
    /// <response code="500">An internal server error occurred while processing the request.</response>
    /// <example>
    ///     GET /api/users/12345678-1234-1234-1234-123456789abc/security/status
    /// </example>
    [HttpGet("status", Name = "GetUserSecurityStatus")]
    [ProducesResponseType(typeof(UserSecurityStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserSecurity)]
    public async Task<IActionResult> GetUserSecurityStatus([FromRoute] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await securityService.GetUserSecurityStatusAsync(userId, cancellationToken);
        return Ok(result);
    }
    
    
    /// <summary>
    ///     Records a failed login attempt for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who had a failed login attempt.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the attempt was recorded successfully; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     This endpoint is typically called by the authentication system when a user provides incorrect credentials.
    ///     The system will track the number of failed attempts and may trigger account locking
    ///     if a configured threshold is exceeded.
    /// </remarks>
    /// <response code="200">The failed login attempt was successfully recorded.</response>
    /// <response code="400">Invalid user ID format or request parameters.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks permission to record security events.</response>
    /// <response code="404">User or user security records were not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     PUT /api/users/12345678-1234-1234-1234-123456789abc/security/failed-attempts
    /// </example>
    [HttpPut("failed-attempts", Name = "RecordFailedLoginAttempt")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserSecurity)]
    public async Task<IActionResult> RecordFailedLoginAttempt([FromRoute] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await securityService.RecordFailedLoginAttemptAsync(userId, cancellationToken);
        return Ok(result);
    }


    /// <summary>
    ///     Resets the failed login attempt counter for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose failed attempt counter should be reset.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the counter was reset successfully; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     This endpoint is typically called after a successful login or by an administrator to clear
    ///     a user’s failed login history.
    /// </remarks>
    /// <response code="200">The failed login attempt counter was successfully reset.</response>
    /// <response code="400">Invalid user ID format.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks permission to manage user security.</response>
    /// <response code="404">User or user security records were not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     PUT /api/users/12345678-1234-1234-1234-123456789abc/security/reset-failed-attempts
    /// </example>
    [HttpPut("reset-failed-attempts", Name = "ResetFailedLoginAttempts")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserSecurity)]
    public async Task<IActionResult> ResetFailedLoginAttempts([FromRoute] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var isLocked = await securityService.ResetFailedLoginAttemptsAsync(userId, cancellationToken);
        return Ok(isLocked);
    }


    /// <summary>
    ///     Checks whether the specified user account is currently locked.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the account is locked; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     This endpoint allows checking if a user account is currently locked due to security policies,
    ///     failed login attempts, or administrative actions.
    /// </remarks>
    /// <response code="200">Successfully retrieved the user's lock status.</response>
    /// <response code="400">Invalid user ID format.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks permission to view user security status.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     GET /api/users/12345678-1234-1234-1234-123456789abc/security/lock-status
    /// </example>
    [HttpGet("is-locked", Name = "IsUserLocked")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    //[RequirePermission(enPermissions.ManageUserSecurity)]
    public async Task<IActionResult> IsUserLocked([FromRoute] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var isLocked = await securityService.IsUserLockedAsync(userId, cancellationToken);
        return Ok(isLocked);
    }


    /// <summary>
    ///     Locks the specified user account until the given expiration date.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to lock.</param>
    /// <param name="request">The lock request containing the duration and optional reason.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Confirmation that the user account was successfully locked.</returns>
    /// <remarks>
    ///     <para>
    ///         This endpoint allows administrators to manually lock user accounts for security purposes,
    ///         such as suspected compromise, policy violations, or ongoing investigations.
    ///     </para>
    ///     <para>
    ///         <strong>Important:</strong> When a user account is locked, all authentication tokens
    ///         are revoked, and the user will be logged out from all devices.
    ///     </para>
    /// </remarks>
    /// <response code="200">User account was successfully locked.</response>
    /// <response code="400">Invalid request parameters or user ID format.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks permission to lock user accounts.</response>
    /// <response code="404">User not found.</response>
    /// <response code="409">User account is already locked.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     PUT /api/users/12345678-1234-1234-1234-123456789abc/security/lock
    /// </example>
    [HttpPut("lock", Name = "LockUser")]
    [ProducesResponseType(typeof(LockUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
   // [RequirePermission(enPermissions.ManageUserSecurity)]
    public async Task<IActionResult> LockUser([FromRoute] Guid userId,
        [FromBody] [Required] LockUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = await securityService.LockUserAsync(userId, request, cancellationToken);
        return Ok(response);
    }


    /// <summary>
    ///     Unlocks the specified user account, removing any lock restrictions.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unlock.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Confirmation that the user account was successfully unlocked.</returns>
    /// <remarks>
    ///     This endpoint allows administrators to manually unlock user accounts that were previously locked
    ///     due to security policies, failed login attempts, or administrative actions. Unlocking allows the
    ///     user to authenticate normally, but they must get new tokens.
    /// </remarks>
    /// <response code="200">User account was successfully unlocked.</response>
    /// <response code="400">Invalid user ID format.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="403">Caller lacks permission to unlock user accounts.</response>
    /// <response code="404">User not found.</response>
    /// <response code="409">User account is not currently locked.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <example>
    ///     PUT /api/users/12345678-1234-1234-1234-123456789abc/security/unlock
    /// </example>
    [HttpPut("unlock", Name = "UnlockUser")]
    [ProducesResponseType(typeof(UnlockUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
  //  [RequirePermission(enPermissions.ManageUserSecurity)]
    public async Task<IActionResult> UnlockUser([FromRoute] Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await securityService.UnlockUserAsync(userId, cancellationToken);
        return Ok(result);
    }
}