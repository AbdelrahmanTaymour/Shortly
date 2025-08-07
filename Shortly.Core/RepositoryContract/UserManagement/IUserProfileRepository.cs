using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

/// <summary>
/// Repository interface for managing user profile data operations.
/// Provides methods for retrieving, updating, and deleting user profiles.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Retrieves a user profile by the associated user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose profile to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user profile if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    /// <remarks>Uses AsNoTracking for optimal read-only performance.</remarks>
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user profile in the database.
    /// </summary>
    /// <param name="profile">The user profile entity with updated information.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when database operation fails.</exception>
    Task<bool> UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);
}