using Shortly.Core.DTOs.UserActionTokensDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.Tokens;

/// <summary>
/// Defins service for managing user action tokens and their lifecycle.
/// Provides high-level operations for token generation, validation, and consumption.
/// </summary>
public interface IUserActionTokenService
{
    /// <summary>
    /// Generates a new action token for a user with automatic expiration handling.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tokenType">The type of token to generate.</param>
    /// <param name="customExpiration">Optional custom expiration time. If null, uses default for token type.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a UserActionTokenDto with the generated token details, including the plain text token.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method automatically invalidates any existing active tokens of the same type for the user
    /// before generating a new one. The returned DTO contains the plain text token which should
    /// be sent to the user, while only the hashed version is stored in the database.
    /// </remarks>
    Task<UserActionTokenDto> GenerateTokenAsync(Guid userId, enUserActionTokenType tokenType, TimeSpan? customExpiration = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a token hash against the expected token type and business rules.
    /// </summary>
    /// <param name="tokenHash">The hashed token string to validate.</param>
    /// <param name="expectedTokenType">The expected type of the token.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if the token is valid and can be used.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when the token is not found in the database.</exception>
    /// <exception cref="ForbiddenException">Thrown when the token type doesn't match or the token has expired.</exception>
    /// <exception cref="ConflictException">Thrown when the token has already been used.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method performs comprehensive validation including existence, type matching,
    /// usage status, and expiration checks. It does not consume the token.
    /// </remarks>
    Task<bool> ValidateTokenAsync(string tokenHash, enUserActionTokenType expectedTokenType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves detailed information about a token using its plain text value.
    /// </summary>
    /// <param name="token">The plain text token to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a UserActionTokenDto with the token's details, including the original plain text token.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when the token is not found in the database.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method hashes the provided plain text token to perform the lookup.
    /// The returned DTO includes the original plain text token for convenience.
    /// </remarks>
    Task<UserActionTokenDto> GetTokenDetailsAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates and consumes a token in a single atomic operation.
    /// </summary>
    /// <param name="token">The plain text token to validate and consume.</param>
    /// <param name="tokenType">The expected type of the token.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if the token was successfully validated and consumed.
    /// </returns>
    /// <exception cref="NotFoundException">Thrown when the token is not found in the database.</exception>
    /// <exception cref="ForbiddenException">Thrown when the token type doesn't match or the token has expired.</exception>
    /// <exception cref="ConflictException">Thrown when the token has already been used.</exception>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method first validates the token against all business rules, then marks it as used.
    /// Once consumed, the token cannot be used again. This is the recommended way to use tokens
    /// for one-time operations like email verification or password reset.
    /// </remarks>
    Task<bool> ConsumeTokenAsync(string token, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates all active tokens of a specific type for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tokenType">The type of tokens to invalidate.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if any tokens were invalidated, false if no active tokens were found.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method is typically called before generating new tokens to ensure
    /// only one active token exists per type per user. Invalidated tokens are marked as used.
    /// </remarks>
    Task<bool> InvalidateUserTokensAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Permanently removes all expired or used tokens from the database.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the number of tokens that were permanently deleted.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method should be called periodically (e.g., via background service) to maintain
    /// database performance by removing tokens that are no longer needed.
    /// Tokens are considered expired if they're marked as used OR have passed their expiration date.
    /// </remarks>
    Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user has any active tokens of a specific type.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tokenType">The type of token to check for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if the user has at least one active token of the specified type, otherwise false.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// A token is considered active if it's not marked as used and hasn't expired.
    /// This method is useful for enforcing business rules like preventing multiple
    /// password reset requests or checking verification status.
    /// </remarks>
    Task<bool> HasActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
}