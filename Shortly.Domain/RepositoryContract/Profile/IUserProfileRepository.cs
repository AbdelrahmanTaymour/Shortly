using Shortly.Domain.Entities;

namespace Shortly.Domain.RepositoryContract.Profile;

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
}