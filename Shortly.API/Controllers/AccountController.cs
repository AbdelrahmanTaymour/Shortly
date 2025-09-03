using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.AuthDTOs.Email;
using Shortly.Core.DTOs.AuthDTOs.Password;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.ServiceContracts.Authentication;

namespace Shortly.API.Controllers;

/// <summary>
/// Controller for managing user account operations including email verification, 
/// password management, and email change functionality.
/// </summary>
/// <param name="accountService">Service for handling account-related operations.</param>
/// <remarks>
/// This controller provides endpoints for critical user account security operations.
/// All sensitive operations use secure token-based verification and follow security 
/// best practices, including protection against user enumeration attacks.
/// 
/// Key security features implemented:
/// - Encrypted tokens for all verification processes
/// - Prevention of user enumeration in password reset flows
/// - Session invalidation after password changes
/// - Two-step verification for email changes
/// </remarks>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AccountController(IAccountService accountService) : ControllerApiBase
{
    /// <summary>
    /// Sends an email verification link to the specified or current user's email address.
    /// </summary>
    /// <param name="request">The request containing the email address (optional if user is authenticated).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether the verification email was sent successfully.</returns>
    /// <example>POST /api/auth/send-email-verification</example>
    /// <remarks>
    /// <para><strong>Access:</strong> This endpoint can be used by both authenticated and unauthenticated users.</para>
    /// <para>If the user is authenticated and no email is provided in the request, the current user's email will be used.</para>
    /// <para>A new verification token is generated, and any existing tokens are invalidated for security.</para>
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/send-email-verification
    ///     {
    ///         "email": "user@example.com"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "emailSent": true,
    ///         "message": "Verification email sent successfully."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Verification email was queued successfully.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="403">User does not have permission to send verification email.</response>
    /// <response code="404">No user was found with the specified email address.</response>
    /// <response code="409">The email address is already verified.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("send-email-verification", Name = "SendEmailVerification")]
    [ProducesResponseType(typeof(SendEmailVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendEmailVerification(
        [FromBody] SendEmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email ?? GetCurrentEmail();
        var result = await accountService.SendVerificationEmailAsync(email, cancellationToken);
        
        var response = new SendEmailVerificationResponse
        (
            EmailSent: result,
            Message: result ? "Verification email sent successfully." : "Failed to send verification email."
        );

        return Ok(response);
    }
    
    /// <summary>
    /// Verifies a user's email address using the provided verification token and returns authentication details.
    /// </summary>
    /// <param name="request">The request containing the encrypted verification token from the email link.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Complete authentication response with user details and JWT tokens.</returns>
    /// <example>POST /api/auth/verify-email</example>
    /// <remarks>
    /// This endpoint completes the email verification process by validating and consuming the token,
    /// marking the user's email as confirmed, and providing authentication tokens for immediate login.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/verify-email
    ///     {
    ///         "token": "encrypted_verification_token_from_email"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "id": "123e4567-e89b-12d3-a456-426614174000",
    ///         "email": "user@example.com",
    ///         "tokens": {
    ///             "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpJVCL9...",
    ///             "refreshToken": "refresh_token_here",
    ///             "expiresAt": "2024-12-31T23:59:59Z"
    ///         },
    ///         "success": true,
    ///         "requiresEmailConfirmation": false
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Email was verified successfully and authentication tokens are provided.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="404">Verification token is not found or invalid.</response>
    /// <response code="409">Token has already been used.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("verify-email", Name = "VerifyEmail")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResponse = await accountService.VerifyEmailAsync(request.Token, cancellationToken);
        return Ok(authResponse);
    }

    /// <summary>
    /// Initiates an email change process by validating the current password and sending a confirmation email to the new address.
    /// </summary>
    /// <param name="request">The request containing the new email address and current password for verification.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether the email change process was successfully initiated.</returns>
    /// <example>POST /api/auth/change-email</example>
    /// <remarks>
    /// <para><strong>Authentication Required:</strong> User must be authenticated to change their email address.</para>
    /// <para>This endpoint begins a two-step email change process for security:</para>
    /// <list type="number">
    ///   <item><description>Validates the user's current password</description></item>
    ///   <item><description>Checks that the new email isn't already in use</description></item>
    ///   <item><description>Sends a confirmation link to the NEW email address</description></item>
    /// </list>
    /// <para>The email change is not completed until the user clicks the confirmation link sent to the new email address.</para>
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/change-email
    ///     {
    ///         "newEmail": "newemail@example.com",
    ///         "password": "current_user_password"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "Email change confirmation sent to your new email address."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Email change process was initiated successfully.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to change email.</response>
    /// <response code="404">Current user not found.</response>
    /// <response code="409">The new email address is already in use by another user.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("change-email", Name = "ChangeEmail")]
    [Authorize]
    [ProducesResponseType(typeof(EmailChangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangeEmail(
        [FromBody] ChangeEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var currentEmail = GetCurrentEmail();
        var result = await accountService.InitiateEmailChangeAsync(
            currentUserId, 
            currentEmail, 
            request.NewEmail, 
            request.Password,
            cancellationToken);

        var response = new EmailChangeResponse
        (
            Success: result,
            Message: result ? "Email change confirmation sent to your new email address." : "Failed to initiate email change."
        );

        return Ok(response);
    }

    /// <summary>
    /// Completes the email change process by validating the confirmation token and updating the user's email address.
    /// </summary>
    /// <param name="request">The request containing the encrypted confirmation token from the email change confirmation link.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether the email change was completed successfully.</returns>
    /// <example>POST /api/auth/confirm-email-change</example>
    /// <remarks>
    /// This endpoint completes the two-step email change verification process. It validates the confirmation token,
    /// performs a final check that the new email is still available, and updates the user's email address.
    /// 
    /// The confirmation token is consumed during this process and cannot be reused.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/confirm-email-change
    ///     {
    ///         "token": "encrypted_confirmation_token_from_email"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "Email address updated successfully."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">The email address was updated successfully.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="404">Confirmation token isn't found or invalid.</response>
    /// <response code="409">The new email address is already in use (double-check protection).</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("confirm-email-change", Name = "ConfirmEmailChange")]
    [ProducesResponseType(typeof(EmailChangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmEmailChange(
        [FromBody] ConfirmEmailChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await accountService.ConfirmEmailChangeAsync(request.Token, cancellationToken);
        
        var response = new EmailChangeResponse
        (
            Success: result,
            Message: result ? "Email address updated successfully." : "Failed to update email address."
        );

        return Ok(response);
    }
    
    /// <summary>
    /// Initiates the password reset process by sending a password reset link to the user's email address.
    /// </summary>
    /// <param name="request">The request containing the email address for password reset.</param> 
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating the reset process was initiated (always returns success to prevent user enumeration).</returns>
    /// <example>POST /api/auth/forgot-password</example>
    /// <remarks>
    /// <para><strong>Security:</strong> This endpoint implements anti-enumeration protection by always returning success,
    /// regardless of whether the email address exists in the system.</para>
    /// 
    /// <para>If the email address exists and the user is active:</para>
    /// <list type="bullet">
    ///   <item><description>A password reset token is generated</description></item>
    ///   <item><description>A reset email is queued for sending</description></item>
    ///   <item><description>The token has a shorter expiration time for enhanced security</description></item>
    /// </list>
    /// 
    /// <para>If the email doesn't exist or the user is inactive/deleted, no email is sent,
    /// but the response is identical to prevent attackers from discovering valid email addresses.</para>
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/forgot-password
    ///     {
    ///         "email": "user@example.com"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "If the email address exists, a password reset link has been sent."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Password reset process was initiated (always returned to prevent user enumeration).</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("forgot-password", Name = "ForgotPassword")]
    [ProducesResponseType(typeof(PasswordOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, 
        CancellationToken cancellationToken = default)
    {
        await accountService.ForgotPasswordAsync(request.Email, cancellationToken);
        
        var response = new PasswordOperationResponse
        (
            Success: true,
            Message: "If the email address exists, a password reset link has been sent."
        );

        return Ok(response);
    }

    /// <summary>
    /// Validates a password reset token without consuming it to check if it's valid for password reset.
    /// </summary>
    /// <param name="request">The request containing the encrypted password reset token to validate.</param> 
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether the token is valid for password reset.</returns>
    /// <example>POST /api/auth/validate-reset-token</example>
    /// <remarks>
    /// This endpoint is typically used by frontend applications to validate reset tokens before 
    /// presenting the password reset form to users. It performs comprehensive validation including
    /// existence, expiration, and usage checks without consuming the token.
    /// 
    /// This allows users to refresh the page or navigate away from the reset form without 
    /// invalidating their reset link.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/validate-reset-token
    ///     {
    ///         "token": "encrypted_reset_token_from_email"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "isValid": true,
    ///         "message": "Token is valid."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Returns whether the token is valid or not.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("validate-reset-token", Name = "ValidateResetToken")]
    [ProducesResponseType(typeof(ValidateTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateResetToken(
        [FromBody] ValidateResetTokenRequest request, 
        CancellationToken cancellationToken = default)
    {
        var result = await accountService.ValidateResetToken(request.Token, cancellationToken);

        var response = new ValidateTokenResponse
        (
            IsValid: result,
            Message: result ? "Token is valid." : "Token is invalid or expired."
        );

        return Ok(response);
    }

    /// <summary>
    /// Resets a user's password using a valid password reset token and revokes all existing user sessions.
    /// </summary>
    /// <param name="request">The request containing the encrypted reset token and new password.</param> 
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether the password was reset successfully.</returns>
    /// <example>POST /api/auth/reset-password</example>
    /// <remarks>
    /// This endpoint performs a complete password reset process:
    /// <list type="number">
    ///   <item><description>Validates and consumes the reset token (marks it as used)</description></item>
    ///   <item><description>Updates the user's password with proper security hashing</description></item>
    ///   <item><description>Revokes all existing user sessions for security</description></item>
    /// </list>
    /// 
    /// After a successful password reset, the user will need to log in again with their new password.
    /// The reset token cannot be reused once consumed.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/reset-password
    ///     {
    ///         "token": "encrypted_reset_token_from_email",
    ///         "newPassword": "new_secure_password_123!"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "Password reset successfully. Please log in with your new password."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Password was reset successfully.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="404">Reset token is not found or invalid.</response>
    /// <response code="409">Token has already been used.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <response code="503">The password update service is temporarily unavailable.</response>
    [HttpPost("reset-password", Name = "ResetPassword")]
    [ProducesResponseType(typeof(PasswordOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request, 
        CancellationToken cancellationToken = default)
    {
        var result = await accountService.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);
        
        var response = new PasswordOperationResponse
        (
            Success: result,
            Message: result ? "Password reset successfully. Please log in with your new password." : "Failed to reset password."
        );

        return Ok(response);
    }
    
    /// <summary>
    /// Changes an authenticated user's password after validating their current password and revokes all existing sessions.
    /// </summary>
    /// <param name="request">The request containing the current password for verification and the new password.</param> 
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Response indicating whether the password was changed successfully.</returns>
    /// <example>POST /api/auth/change-password</example>
    /// <remarks>
    /// <para><strong>Authentication Required:</strong> User must be authenticated to change their password.</para>
    /// 
    /// <para>This endpoint is used for authenticated password changes (as opposed to password resets via email).
    /// It requires the user to provide their current password for security verification.</para>
    /// 
    /// <para>Security measures implemented:</para>
    /// <list type="bullet">
    ///   <item><description>Current password validation before allowing change</description></item>
    ///   <item><description>All user sessions are revoked after a successful password change</description></item>
    ///   <item><description>User must re-authenticate with the new password</description></item>
    /// </list>
    /// 
    /// <para>This prevents session hijacking if the password change was initiated due to a security concern.</para>
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/change-password
    ///     {
    ///         "currentPassword": "old_password_123",
    ///         "newPassword": "new_secure_password_456!",
    ///         "confirmPassword": "new_secure_password_456!"
    ///     }
    /// 
    /// Sample response:
    /// 
    ///     {
    ///         "success": true,
    ///         "message": "Password changed successfully. All sessions have been logged out."
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">The password was changed successfully.</response>
    /// <response code="400">The request data is invalid or malformed.</response>
    /// <response code="401">User is not authenticated or the current password is invalid.</response>
    /// <response code="404">Current user not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    /// <response code="503">The password update service is temporarily unavailable.</response>
    [HttpPost("change-password", Name = "ChangePassword")]
    [Authorize]
    [ProducesResponseType(typeof(PasswordOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request, 
        CancellationToken cancellationToken = default)
    {
        var result = await accountService.ChangePasswordAsync(
            GetCurrentUserId(), 
            request.CurrentPassword,
            request.NewPassword, 
            cancellationToken);
        
        var response = new PasswordOperationResponse
        (
            Success: result,
            Message: result ? "Password changed successfully. All sessions have been logged out." : "Failed to change password."
        );

        return Ok(response);
    }
}