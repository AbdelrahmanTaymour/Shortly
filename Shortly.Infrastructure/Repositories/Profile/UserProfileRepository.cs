using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.RepositoryContract.Profile;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.Profile;

/// <summary>
/// SQL Server implementation of the user profile repository.
/// </summary>
/// <remarks>
/// Uses Entity Framework Core with SQL Server for data access.
/// </remarks>
public class UserProfileRepository(SqlServerDbContext dbContext, ILogger<UserProfileRepository> logger)
    : IUserProfileRepository
{
    /// <inheritdoc/>
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.UserId == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user profile for user ID: {UserId}", userId);
            throw new DatabaseException("Failed to retrieve user profile", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.Update(profile);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile for user ID: {UserId}", profile.UserId);
            throw new DatabaseException("Failed to update user profile", ex);
        }
    }
}