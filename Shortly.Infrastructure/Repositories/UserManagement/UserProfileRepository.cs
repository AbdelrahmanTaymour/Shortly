using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

public class UserProfileRepository(SQLServerDbContext dbContext, ILogger<UserProfileRepository> logger) : IUserProfileRepository
{
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

    public async Task<bool> DeleteAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.UserProfiles.Remove(profile);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user profile for user ID: {UserId}", profile.UserId);
            throw new DatabaseException("Failed to delete user profile", ex);
        }
    }
}