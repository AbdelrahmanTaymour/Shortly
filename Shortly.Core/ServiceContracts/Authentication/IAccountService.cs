using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;

namespace Shortly.Core.ServiceContracts.Authentication;

/// <summary>
/// Service responsible for managing user account operations including email verification,
/// password management, and email change functionality.
/// </summary>
/// <remarks>
/// This service handles critical user account operations that require secure token-based verification.
/// All operations involving sensitive actions (password resets, email changes) use encrypted tokens
/// and follow security best practices to prevent user enumeration attacks.
/// </remarks>
public interface IAccountService
{
    /// <summary>
    /// Sends an email verification link to the specified email address.
    /// </summary>
    /// <param name="email">The email address to send the verification link to.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>true if the verification email was successfully queued for sending.</returns>
    /// <exception cref="ConflictException">Thrown when the email address is already verified for this user.</exception>
    /// <exception cref="NotFoundException">Thrown when no user is found with the specified email address.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method generates a new email verification token and invalidates any existing ones.
    /// The verification link contains an encrypted token that expires based on configuration settings.
    /// The email is queued for background processing to avoid blocking the request.
    /// </remarks>
    Task<bool> SendVerificationEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a user's email address using the provided verification token and returns authentication details.
    /// </summary>
    /// <param name="token">The encrypted verification token from the email link.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// an <see cref="AuthenticationResponse"/> with user details and authentication tokens.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when the verification token is not found or invalid.</exception>
    /// <exception cref="ConflictException">Thrown when the token has already been used.</exception>
    /// <exception cref="ForbiddenException">Thrown when the verification token has expired.</exception>
    /// <exception cref="ValidationException">Thrown when the token type doesn't match the expected email verification type.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method consumes the verification token (marks it as used) and updates the user's
    /// email confirmation status. Upon successful verification, it generates new authentication
    /// tokens and returns a complete authentication response for immediate user login.
    /// </remarks>
    Task<AuthenticationResponse> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates an email change process by validating the current password
    /// and sending a confirmation email to the new address.
    /// </summary>
    /// <param name="userId">The unique identifier of the user requesting the email change.</param>
    /// <param name="currentEmail">The user's current email address (for validation).</param>
    /// <param name="newEmail">The new email address to change to.</param>
    /// <param name="currentPassword">The user's current password for security verification.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>true if the email change process was successfully initiated.</returns>
    /// <exception cref="ConflictException">Thrown when the new email address is already in use by another user.</exception>
    /// <exception cref="UnauthorizedException">Thrown when the provided current password is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method performs several security checks: validates the current password, ensures
    /// the new email is not already in use, and generates a secure token for confirmation.
    /// A confirmation email is sent to the NEW email address to verify ownership.
    /// The email change is not completed until the user clicks the confirmation link.
    /// </remarks>
    Task<bool> InitiateEmailChangeAsync(Guid userId, string currentEmail, string newEmail, string currentPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the email change process by validating the confirmation token and updating the user's email.
    /// </summary>
    /// <param name="token">The encrypted confirmation token from the email change confirmation link.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if the email change was successfully completed.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the confirmation token is not found or invalid.
    /// </exception>
    /// <exception cref="ConflictException">
    /// Thrown when the new email address is already in use (double-check protection).
    /// </exception>
    /// <exception cref="DatabaseException">
    /// Thrown when database operation fails.
    /// </exception>
    /// <remarks>
    /// This method validates the email change token, performs a final check that the new email
    /// is still available, and updates the user's email address in the database. The token is
    /// consumed during this process and cannot be reused. This completes the two-step email
    /// change verification process.
    /// </remarks>
    Task<bool> ConfirmEmailChangeAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates the password reset process by sending a password reset link to the user's email.
    /// </summary>
    /// <param name="email">The email address of the user requesting a password reset.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>true to prevent user enumeration attacks.</returns>
    /// <remarks>
    /// <para>
    /// This method implements security best practices to prevent user enumeration:
    /// - Always returns true regardless of whether the email exists
    /// - Only sends reset emails to active, non-deleted users
    /// - Logs warnings for errors without exposing them to the caller
    /// </para>
    /// <para>
    /// If the user exists and is active, a password reset token is generated, and a reset email
    /// is queued for sending. The token has a shorter expiration time than verification tokens
    /// for enhanced security.
    /// </para>
    /// </remarks>
    Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a password reset token without consuming it.
    /// </summary>
    /// <param name="token">The encrypted password reset token to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>true if the token is valid and can be used for password reset, false otherwise.</returns>
    /// <remarks>
    /// This method is typically used by frontend applications to validate reset tokens
    /// before presenting the password reset form to users. It performs the same validation
    /// as the reset process but doesn't consume the token, allowing users to refresh the
    /// page or navigate away without invalidating their reset link.
    /// 
    /// Returns false for any error to prevent information disclosure about token validity.
    /// </remarks>
    Task<bool> ValidateResetToken(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password using a valid password reset token.
    /// </summary>
    /// <param name="token">The encrypted password reset token from the reset email.</param>
    /// <param name="newPassword">The new password to set for the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>true if the password was successfully reset.</returns>
    /// <exception cref="NotFoundException">Thrown when the reset token is not found or invalid.</exception>
    /// <exception cref="ConflictException">Thrown when the token has already been used.</exception>
    /// <exception cref="ForbiddenException">Thrown when the reset token has expired.</exception>
    /// <exception cref="ValidationException">Thrown when the token type doesn't match the expected password reset type.</exception>
    /// <exception cref="ServiceUnavailableException">Thrown when the password update operation fails due to system issues. </exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method performs a complete password reset process: validates and consumes the token,
    /// updates the user's password with proper hashing, and revokes all existing user sessions
    /// for security. The user will need to log in again with their new password.
    /// </remarks>
    Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password after validating their current password.
    /// </summary>
    /// <param name="userId">The unique identifier of the user changing their password.</param>
    /// <param name="currentPassword">The user's current password for verification.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>true if the password was successfully changed.</returns>
    /// <exception cref="UnauthorizedException">Thrown when the provided current password is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the user is not found.</exception>
    /// <exception cref="ServiceUnavailableException">Thrown when the password update operation fails due to system issues.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method is used for authenticated password changes (as opposed to password resets).
    /// It requires the user to provide their current password for security verification.
    /// Upon successful password change, all user sessions are revoked, requiring re-authentication
    /// with the new password. This prevents session hijacking if the password change was
    /// initiated due to a security concern.
    /// </remarks>
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}