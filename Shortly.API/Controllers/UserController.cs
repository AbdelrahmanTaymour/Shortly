using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.Core.DTOs.UserDTOs;
using Shortly.Core.ServiceContracts;
using Shortly.Domain.Enums;
using System.Security.Claims;

namespace Shortly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #region Profile Management

    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var profile = await _userService.GetUserProfile(userId);
            if (profile == null)
                return NotFound("User profile not found.");

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, "An error occurred while retrieving the profile.");
        }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _userService.UpdateUserProfile(userId, request);
            if (!success)
                return BadRequest("Failed to update profile.");

            return Ok(new { message = "Profile updated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, "An error occurred while updating the profile.");
        }
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _userService.DeleteUserAccount(userId);
            if (!success)
                return BadRequest("Failed to delete account.");

            return Ok(new { message = "Account deleted successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account");
            return StatusCode(500, "An error occurred while deleting the account.");
        }
    }

    #endregion

    #region Password Management

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _userService.ChangePassword(userId, request);
            if (!success)
                return BadRequest("Failed to change password. Please check your current password.");

            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, "An error occurred while changing the password.");
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            await _userService.ForgotPassword(request);
            return Ok(new { message = "If the email exists, a password reset link has been sent." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request");
            return StatusCode(500, "An error occurred while processing the request.");
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var success = await _userService.ResetPassword(request);
            if (!success)
                return BadRequest("Invalid reset token or email.");

            return Ok(new { message = "Password reset successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(500, "An error occurred while resetting the password.");
        }
    }

    #endregion

    #region Email Verification

    [HttpPost("send-email-verification")]
    public async Task<IActionResult> SendEmailVerification()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _userService.SendEmailVerification(userId);
            if (!success)
                return BadRequest("Email is already verified or user not found.");

            return Ok(new { message = "Verification email sent successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email verification");
            return StatusCode(500, "An error occurred while sending the verification email.");
        }
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request)
    {
        try
        {
            var success = await _userService.VerifyEmail(request);
            if (!success)
                return BadRequest("Invalid verification token or email.");

            return Ok(new { message = "Email verified successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email");
            return StatusCode(500, "An error occurred while verifying the email.");
        }
    }

    [HttpPost("resend-email-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendEmailVerification([FromBody] ResendEmailVerificationRequest request)
    {
        try
        {
            var success = await _userService.ResendEmailVerification(request);
            if (!success)
                return BadRequest("Email is already verified or user not found.");

            return Ok(new { message = "Verification email resent successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending email verification");
            return StatusCode(500, "An error occurred while resending the verification email.");
        }
    }

    #endregion

    #region Two-Factor Authentication

    [HttpPost("2fa/setup")]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var response = await _userService.SetupTwoFactor(userId);
            if (response == null)
                return BadRequest("Failed to setup two-factor authentication.");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up two-factor authentication");
            return StatusCode(500, "An error occurred while setting up two-factor authentication.");
        }
    }

    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorSetupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _userService.EnableTwoFactor(userId, request);
            if (!success)
                return BadRequest("Invalid verification code.");

            return Ok(new { message = "Two-factor authentication enabled successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling two-factor authentication");
            return StatusCode(500, "An error occurred while enabling two-factor authentication.");
        }
    }

    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var success = await _userService.DisableTwoFactor(userId, request);
            if (!success)
                return BadRequest("Invalid password or two-factor code.");

            return Ok(new { message = "Two-factor authentication disabled successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication");
            return StatusCode(500, "An error occurred while disabling two-factor authentication.");
        }
    }

    [HttpPost("2fa/generate-backup-codes")]
    public async Task<ActionResult<string[]>> GenerateBackupCodes()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var backupCodes = await _userService.GenerateBackupCodes(userId);
            if (backupCodes.Length == 0)
                return BadRequest("Two-factor authentication is not enabled.");

            return Ok(backupCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating backup codes");
            return StatusCode(500, "An error occurred while generating backup codes.");
        }
    }

    #endregion

    #region Usage Statistics

    [HttpGet("usage")]
    public async Task<ActionResult<object>> GetUsageStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var canCreateMore = await _userService.CanCreateMoreLinks(userId);
            var remainingLinks = await _userService.GetRemainingLinksForMonth(userId);

            return Ok(new
            {
                canCreateMoreLinks = canCreateMore,
                remainingLinksThisMonth = remainingLinks
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage statistics");
            return StatusCode(500, "An error occurred while retrieving usage statistics.");
        }
    }

    #endregion

    #region Admin Operations (requires admin role)

    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<ActionResult<object>> GetAllUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] enUserRole? role = null,
        [FromQuery] enSubscriptionPlan? subscriptionPlan = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? emailVerified = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var (users, totalCount) = await _userService.SearchUsers(
                searchTerm, role, subscriptionPlan, isActive, emailVerified, page, pageSize);

            return Ok(new
            {
                users = users,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, "An error occurred while retrieving users.");
        }
    }

    [HttpPost("{userId:guid}/lock")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> LockUser(Guid userId, [FromBody] DateTime? lockUntil = null)
    {
        try
        {
            var success = await _userService.LockUserAccount(userId, lockUntil);
            if (!success)
                return BadRequest("Failed to lock user account.");

            return Ok(new { message = "User account locked successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user account");
            return StatusCode(500, "An error occurred while locking the user account.");
        }
    }

    [HttpPost("{userId:guid}/unlock")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> UnlockUser(Guid userId)
    {
        try
        {
            var success = await _userService.UnlockUserAccount(userId);
            if (!success)
                return BadRequest("Failed to unlock user account.");

            return Ok(new { message = "User account unlocked successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user account");
            return StatusCode(500, "An error occurred while unlocking the user account.");
        }
    }

    [HttpPost("{userId:guid}/activate")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        try
        {
            var success = await _userService.ActivateUser(userId);
            if (!success)
                return BadRequest("Failed to activate user.");

            return Ok(new { message = "User activated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user");
            return StatusCode(500, "An error occurred while activating the user.");
        }
    }

    [HttpPost("{userId:guid}/deactivate")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        try
        {
            var success = await _userService.DeactivateUser(userId);
            if (!success)
                return BadRequest("Failed to deactivate user.");

            return Ok(new { message = "User deactivated successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user");
            return StatusCode(500, "An error occurred while deactivating the user.");
        }
    }

    [HttpGet("analytics")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<ActionResult<object>> GetUserAnalytics()
    {
        try
        {
            var totalUsers = await _userService.GetTotalUsersCount();
            var activeUsers = await _userService.GetActiveUsersCount();
            var freeUsers = await _userService.GetUsersCountByPlan(enSubscriptionPlan.Free);
            var paidUsers = totalUsers - freeUsers;

            return Ok(new
            {
                totalUsers = totalUsers,
                activeUsers = activeUsers,
                inactiveUsers = totalUsers - activeUsers,
                freeUsers = freeUsers,
                paidUsers = paidUsers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user analytics");
            return StatusCode(500, "An error occurred while retrieving user analytics.");
        }
    }

    #endregion
}