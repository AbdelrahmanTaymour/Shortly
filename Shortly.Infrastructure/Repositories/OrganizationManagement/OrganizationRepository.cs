using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

public class OrganizationRepository(SQLServerDbContext dbContext, ILogger<OrganizationRepository> logger)
    : IOrganizationRepository
{
    /// <inheritdoc/>
    public async Task<bool> IsUserOwnerOfAnyOrganization(Guid userId)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .AnyAsync(o => o.OwnerId == userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user with ID {UserId} owns any organization.", userId);
            throw new DatabaseException("An error occurred while checking organization ownership.", ex);
        }
    }
}