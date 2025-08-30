using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.RepositoryContract.Tokens;

/// <summary>
/// Repository  for managing user action tokens in the database.
/// Handles CRUD operations and token lifecycle management for various user action tokens.
/// </summary>
public interface IUserActionTokenRepository
{
    /// <summary>
    /// Retrieves a user action token by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the token.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the UserActionToken entity with associated User if found, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>This method includes the associated User entity in the result.</remarks>
    Task<UserActionToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves a user action token by its hashed token value.
    /// </summary>
    /// <param name="tokenHash">The hashed token string to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the UserActionToken entity with associated User if found, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method includes the associated User entity in the result.
    /// The tokenHash parameter should be the SHA256 hash of the original token.
    /// </remarks>
    Task<UserActionToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an active (unused and not expired) token for a specific user and token type.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tokenType">The type of token to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the active UserActionToken entity with associated User if found, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// A token is considered active if it's not marked as used and hasn't expired.
    /// This method includes the associated User entity in the result.
    /// </remarks>
    Task<UserActionToken?> GetActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all active tokens for a specific user across all token types.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a collection of active UserActionToken entities for the specified user.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Returns tokens that are not marked as used and haven't expired.
    /// Uses AsNoTracking() for read-only operations to improve performance.
    /// </remarks>
    Task<IEnumerable<UserActionToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all expired or used tokens from the database.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a collection of expired or used UserActionToken entities.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// A token is considered expired if it's marked as used OR has passed its expiration date.
    /// Uses AsNoTracking() for read-only operations to improve performance.
    /// Typically used for cleanup operations.
    /// </remarks>
    Task<IEnumerable<UserActionToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user action token in the database.
    /// </summary>
    /// <param name="token">The UserActionToken entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the created UserActionToken entity with any database-generated values populated.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// The method will save changes to the database and return the created entity.
    /// Ensure the token has all required properties set before calling this method.
    /// </remarks>
    Task<UserActionToken> CreateAsync(UserActionToken token, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Updates an existing user action token in the database.
    /// </summary>
    /// <param name="token">The UserActionToken entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the updated UserActionToken entity.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// The entity must be tracked by the context or have its ID set for the update to work.
    /// All changes will be persisted to the database.
    /// </remarks>
    Task<UserActionToken> UpdateAsync(UserActionToken token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks a token as consumed (used) based on its hash and type.
    /// </summary>
    /// <param name="tokenHash">The hashed token string to consume.</param>
    /// <param name="tokenType">The expected type of the token.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if a token was successfully consumed, false if no matching token was found.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Only unused tokens matching the hash and type will be consumed.
    /// This is an atomic operation that updates the Used flag directly in the database.
    /// </remarks>
    Task<bool> ConsumeTokenAsync(string tokenHash, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes a token by marking it as used.
    /// </summary>
    /// <param name="id">The unique identifier of the token to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if a token was successfully marked as used, false if no matching token was found.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method performs a soft delete by setting the Used flag to true.
    /// The token record remains in the database but becomes inactive.
    /// </remarks>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Permanently removes all expired or used tokens from the database.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the number of tokens that were deleted.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method performs a hard delete, permanently removing records from the database.
    /// Tokens are considered expired if they're marked as used OR have passed their expiration date.
    /// Use this method for periodic cleanup to maintain database performance.
    /// </remarks>
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates all active tokens of a specific type for a user by marking them as used.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="tokenType">The type of tokens to invalidate.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// true if any tokens were invalidated, false if no matching active tokens were found.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// Only unused tokens matching the user ID and token type will be invalidated.
    /// This is useful when generating new tokens to ensure only one active token exists per type per user.
    /// </remarks>
    Task<bool> InvalidateUserTokensAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default);
    
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
    /// Uses AsNoTracking() for read-only operations to improve performance.
    /// Useful for preventing duplicate token generation or enforcing business rules.
    /// </remarks>
    Task<bool> HasActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken);
}