using Shortly.Core.DTOs.TokenDTOs;
using Shortly.Core.Exceptions.ClientErrors;

namespace Shortly.Core.ServiceContracts.Tokens;

/// <summary>
/// Service interface for managing email change token business operations.
/// </summary>
public interface IEmailChangeTokenService
{
    /// <summary>
    /// Retrieves an email change token by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the email change token.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// An <see cref="EmailChangeTokenDto"/> containing the token information.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no email change token is found for the specified ID.</exception>
    Task<EmailChangeTokenDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an active email change token by its token string.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// An <see cref="EmailChangeTokenDto"/> containing the valid token information.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no valid email change token is found for the specified token string.</exception>
    /// <remarks>Only returns tokens that are not used and have not expired.</remarks>
    Task<EmailChangeTokenDto> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the active email change token for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// An <see cref="EmailChangeTokenDto"/> containing the active token information for the user.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no active email change token is found for the specified user.</exception>
    /// <remarks>Only returns tokens that are not used and have not expired.</remarks>
    Task<EmailChangeTokenDto> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all email change tokens for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A collection of <see cref="EmailChangeTokenDto"/> containing all tokens for the user, ordered by creation date (newest first).
    /// </returns>
    Task<IEnumerable<EmailChangeTokenDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new email change token for a user's email address change request.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <param name="oldEmail">The email that willing to change.</param>
    /// <param name="newEmail">The new email.</param>
    /// <returns>
    /// An <see cref="EmailChangeTokenDto"/> containing the newly created token information.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the user already has an active email change token.</exception>
    /// <remarks>Only one active email change token is allowed per user at a time.</remarks>
    Task<EmailChangeTokenDto> CreateTokenAsync(Guid userId, string oldEmail, string newEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uses an email change token to complete the email address change process.
    /// </summary>
    /// <param name="token">The token string to use for the email change.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// An <see cref="EmailChangeTokenDto"/> containing the used token information.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when no valid email change token is found for the specified token string.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token has already been used or has expired.</exception>
    /// <remarks>This method marks the token as used and sets the UsedAt timestamp.</remarks>
    Task<EmailChangeTokenDto> UseTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an email change token from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the email change token to delete.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A boolean value indicating whether the deletion was successful.
    /// </returns>
    Task<bool> DeleteTokenAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all expired email change tokens from the system as part of maintenance operations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// The number of expired tokens that were successfully removed from the system.
    /// </returns>
    /// <remarks>This method should be called periodically to clean up expired tokens and maintain database performance.</remarks>
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}