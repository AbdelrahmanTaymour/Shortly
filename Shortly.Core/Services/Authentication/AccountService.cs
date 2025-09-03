using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Extensions;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.Email;
using Shortly.Core.ServiceContracts.Tokens;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.Authentication;

/// <summary>
/// Service responsible for managing user account operations including email verification,
/// password management, and email change functionality.
/// </summary>
/// <param name="actionTokenService">Service for managing user action tokens (verification, reset, etc.).</param>
/// <param name="userService">Service for user management operations.</param>
/// <param name="tokenService">Service for JWT authentication token management.</param>
/// <param name="emailChangeTokenService">Specialized service for handling email change tokens.</param>
/// <param name="notificationService">Service for sending email notifications.</param>
/// <param name="logger">Logger instance for tracking operations and errors.</param>
/// <remarks>
/// This service implements critical security operations for user account management.
/// All sensitive operations use encrypted tokens and follow security best practices
/// including prevention of user enumeration attacks and proper session management.
/// 
/// Key security features:
/// - All tokens are encrypted before transmission
/// - Password operations revoke existing sessions
/// - Email changes require two-step verification
/// - User enumeration protection in password reset flows
/// </remarks>
public class AccountService(
    IUserActionTokenService actionTokenService,
    IUserService userService,
    ITokenService tokenService,
    IEmailChangeTokenService emailChangeTokenService,
    IEmailNotificationService notificationService,
    ILogger<AccountService> logger
) : IAccountService
{
    /// <inheritdoc />
    public async Task<bool> SendVerificationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userService.GetByEmailAsync(email, cancellationToken);
        if (user.IsEmailConfirmed)
            throw new ConflictException($"This email '{email}' already confirmed.");

        var userActionToken = await actionTokenService.GenerateTokenAsync(user.Id,
            enUserActionTokenType.EmailVerification, cancellationToken: cancellationToken);

        notificationService.EnqueueSendEmailVerificationAsync(email, user.Username,
            Sha256Extensions.Encrypt(userActionToken.PlainToken));
        
        return true;
    }

    /// <inheritdoc />
    public async Task<AuthenticationResponse> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var decryptToken = Sha256Extensions.Decrypt(token);
        
        // ConsumeTokenAsync method validates the token internally
        var actionTokenDto = await actionTokenService.ConsumeTokenAsync(decryptToken, enUserActionTokenType.EmailVerification, cancellationToken);
        logger.LogInformation("Email verification token consumed for user {UserId}", actionTokenDto.UserId);
        
        await userService.MarkEmailAsConfirmedAsync(actionTokenDto.UserId, cancellationToken);
        logger.LogInformation("Email for user {UserId} verified successfully", actionTokenDto.UserId);
        
        var user = await userService.GetByIdAsync(actionTokenDto.UserId, cancellationToken);

        var tokens = await tokenService.GenerateTokensAsync(user);

        return new AuthenticationResponse(
            Id: user.Id,
            Email: user.Email, 
            Tokens: tokens, 
            Success: true,
            RequiresEmailConfirmation:
            !user.IsEmailConfirmed
            );
    }

    /// <inheritdoc />
    public async Task<bool> InitiateEmailChangeAsync(Guid userId, string currentEmail, string newEmail, string currentPassword,
        CancellationToken cancellationToken = default)
    {
        if(await userService.IsEmailAvailableAsync(newEmail, cancellationToken) == false)
            throw new ConflictException("New email address is already in use.");

        // Validate the user 
        var userDto = await userService.ValidateCurrentPasswordAsync(userId, currentPassword, cancellationToken);
        if (userDto == null)
            throw new UnauthorizedException("Invalid password.");
        
        // Generate token for email change
        var createdEmailToken = await emailChangeTokenService.CreateTokenAsync(userId, currentEmail, newEmail, cancellationToken);
        
        // Send confirmation email to new email address
        notificationService.EnqueueEmailChangeConfirmationAsync(newEmail, userDto.Username, Sha256Extensions.Encrypt(createdEmailToken.Token));
        
        logger.LogInformation("Email change initiated for user: {UserId}, New email: {NewEmail}", userId, newEmail);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmEmailChangeAsync(string token, CancellationToken cancellationToken = default)
    {
        // Validate the token and marks the token as used
        var emailChangeTokenDto = await emailChangeTokenService.UseTokenAsync(Sha256Extensions.Decrypt(token), cancellationToken);
        
        // Double-check email availability
        if (await userService.IsEmailAvailableAsync(emailChangeTokenDto.NewEmail, cancellationToken) == false)
            throw new ConflictException("New email address is already in use.");
        
        // Update user email
        var success = await userService.ChangeEmailAsync(emailChangeTokenDto.UserId, emailChangeTokenDto.NewEmail, cancellationToken);
        logger.LogInformation("Email change confirmed for user: {UserId}, New email: {NewEmail}", emailChangeTokenDto.UserId, emailChangeTokenDto.NewEmail);
        
        return success;
    }

    /// <inheritdoc />
    public async Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userService.GetByEmailAsync(email, cancellationToken);
            if (user.IsDeleted || !user.IsActive)
                return true; // to avoid user enumeration

            // Generate token for password reset
            var actionToken = await actionTokenService.GenerateTokenAsync(user.Id, enUserActionTokenType.PasswordReset,
                cancellationToken: cancellationToken);
            
            notificationService.EnqueueSendPasswordResetAsync(user.Email, user.Username,
                Sha256Extensions.Encrypt(actionToken.PlainToken));
            
            return true;
        }
        catch(Exception ex) when (ex is not DatabaseException)
        {
            logger.LogWarning(ex, "Error in ForgotPasswordAsync for email: {Email}", email);
            return true; // Always return true to avoid user enumeration
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateResetToken(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            await actionTokenService.ValidateTokenAsync(Sha256Extensions.Decrypt(token),
                enUserActionTokenType.PasswordReset, cancellationToken);
            return true;
        }
        catch(Exception ex) when (ex is not DatabaseException)
        {
            logger.LogWarning(ex, "Error while validating reset token");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        // Validate and marks token as used
        var actionTokenDto = await actionTokenService.ConsumeTokenAsync(Sha256Extensions.Decrypt(token), enUserActionTokenType.PasswordReset, cancellationToken);
        
        // Update the user's password in DB
        var success = await userService.ChangePasswordAsync(actionTokenDto.UserId, newPassword, cancellationToken);
        if (!success)
            throw new ServiceUnavailableException("Failed to reset password. Please try resetting password later.");
        
        // Revoke all user sessions
        await tokenService.RevokeAllUserTokensAsync(actionTokenDto.UserId, cancellationToken);

        return success;
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken = default)
    {
        if(await userService.ValidateCurrentPasswordAsync(userId, currentPassword, cancellationToken) == null)
            throw new UnauthorizedException("Invalid current password.");
        
        // Update the user's password in DB
        var success = await userService.ChangePasswordAsync(userId, newPassword, cancellationToken);
        if (!success)
            throw new ServiceUnavailableException("Failed to change password. Please try again later.");

        // Revoke all user sessions
        await tokenService.RevokeAllUserTokensAsync(userId, cancellationToken);
        
        return success;
    }
}