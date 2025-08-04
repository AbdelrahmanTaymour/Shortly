using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.UserManagement;

/// <summary>
/// Repository interface for managing user usage tracking data operations.
/// Provides methods for tracking and managing user resource consumption including links and QR codes.
/// </summary>
public interface IUserUsageRepository
{
    /// <summary>
    /// Retrieves user usage information by the associated user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose usage information to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The user usage information if found; otherwise, null.</returns>
    Task<UserUsage?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates user usage information in the database.
    /// </summary>
    /// <param name="usage">The user usage entity with updated information.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateAsync(UserUsage usage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increments both monthly and total link creation counters for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the increment was successful; otherwise, false.</returns>
    Task<bool> IncrementLinksCreatedAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Increments both monthly and total QR code creation counters for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the increment was successful; otherwise, false.</returns>
    Task<bool> IncrementQrCodesCreatedAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resets the monthly usage counters (links and QR codes) to zero for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose monthly usage should be reset.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the reset was successful; otherwise, false.</returns>
    Task<bool> ResetMonthlyUsageAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all users whose monthly reset date is due or past the specified date.
    /// Used for batch processing of monthly usage resets.
    /// </summary>
    /// <param name="date">The date to compare against monthly reset dates.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of user usage records that need monthly reset processing.</returns>
    Task<IEnumerable<UserUsage>> GetUsersForMonthlyResetAsync(DateTime date, CancellationToken cancellationToken = default);

}