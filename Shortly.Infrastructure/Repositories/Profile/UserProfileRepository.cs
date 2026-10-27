using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.RepositoryContract.Profile;

namespace Shortly.Infrastructure.Repositories.Profile;

/// <summary>
/// SQL Server implementation of the user profile repository.
/// </summary>
/// <remarks>
/// Uses Entity Framework Core with SQL Server for data access.
/// </remarks>
public class UserProfileRepository(ApplicationDbContext dbContext, ILogger<UserProfileRepository> logger)
    : IUserProfileRepository
{
    /// <inheritdoc/>
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Set<UserProfile>()
                .FindAsync([userId], cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user profile for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to retrieve user profile", ex);
        }
    }
}