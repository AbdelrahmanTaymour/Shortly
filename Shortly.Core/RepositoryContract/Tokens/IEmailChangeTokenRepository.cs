using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.Tokens;

/// <summary>
/// Repository interface for managing email change token data operations.
/// </summary>
public interface IEmailChangeTokenRepository
{
    /// <summary>
    /// Retrieves an email change token by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the email change token.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the EmailChangeToken entity with associated User if found, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>This method includes the associated User entity in the result.</remarks>
    Task<EmailChangeToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an active email change token by its token string.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the EmailChangeToken entity with associated User if found and valid, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method only returns tokens that are not used and have not expired.
    /// Includes the associated User entity in the result.
    /// </remarks>
    Task<EmailChangeToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the active email change token for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the active EmailChangeToken entity with associated User if found, otherwise null.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>
    /// This method only returns tokens that are not used and have not expired.
    /// Includes the associated User entity in the result.
    /// </remarks>
    Task<EmailChangeToken?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all email change tokens for a specific user, ordered by creation date descending.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a collection of EmailChangeToken entities with associated User, ordered by creation date (newest first).
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>This method includes the associated User entity for each token in the result.</remarks>
    Task<IEnumerable<EmailChangeToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new email change token in the database.
    /// </summary>
    /// <param name="emailChangeToken">The email changes the token entity to create.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the created EmailChangeToken entity with populated database-generated values.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>The CreatedAt property will be automatically set to the current UTC time.</remarks>
    Task<EmailChangeToken> CreateAsync(EmailChangeToken emailChangeToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing email change token in the database.
    /// </summary>
    /// <param name="emailChangeToken">The email change token entity with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the update operation was successful.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> UpdateAsync(EmailChangeToken emailChangeToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an email change token as used by setting IsUsed to true and UsedAt to current time.
    /// </summary>
    /// <param name="id">The unique identifier of the email change token to mark as used.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the token was successfully marked as used.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>This method sets IsUsed to true and UsedAt to the current UTC time.</remarks>
    Task<bool> UseTokenAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an email change token from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the email change token to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result indicates
    /// whether the deletion was successful.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all expired email change tokens from the database.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the number of expired tokens that were deleted.
    /// </returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>This method removes all tokens where ExpiresAt is less than or equal to the current UTC time.</remarks>
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}