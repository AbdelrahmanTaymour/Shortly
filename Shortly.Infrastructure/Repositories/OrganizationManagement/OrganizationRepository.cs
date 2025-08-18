using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

public class OrganizationRepository(SQLServerDbContext dbContext, ILogger<OrganizationRepository> logger)
    : IOrganizationRepository
{
    public async Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all organizations.");
            throw new DatabaseException("An error occurred while retrieving organizations.", ex);
        }
    }

    public async Task<Organization?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == userId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization with ID {OrganizationId}.", userId);
            throw new DatabaseException("An error occurred while retrieving the organization.", ex);
        }
    }

    public async Task<Organization> AddAsync(Organization organization)
    {
        try
        {
            await dbContext.Organizations.AddAsync(organization);
            await dbContext.SaveChangesAsync();
            return organization;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding organization with name {OrganizationName}.", organization.Name);
            throw new DatabaseException("An error occurred while adding the organization.", ex);
        }
    }

    public async Task<bool> UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.Organizations.Update(organization);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating organization with ID {OrganizationId}.", organization.Id);
            throw new DatabaseException("An error occurred while updating the organization.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == organizationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.DeletedAt, DateTime.UtcNow)
                        .SetProperty(o => o.IsActive, false)
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting organization with ID {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while deleting the organization.", ex);
        }
    }

    public async Task<bool> ExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .AnyAsync(o => o.Id == organizationId, cancellationToken); // Deleted organization included
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if organization with ID {OrganizationId} exists.", organizationId);
            throw new DatabaseException("An error occurred while checking organization existence.", ex);
        }
    }

    public async Task<Organization?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Include(o => o.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization with members for ID {OrganizationId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization with members.", ex);
        }
    }

    public async Task<Organization?> GetByIdWithTeamsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Include(o => o.Teams)
                .ThenInclude(t => t.TeamMembers)
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization with teams for ID {OrganizationId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization with teams.", ex);
        }
    }

    public async Task<Organization?> GetByIdWithFullDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Include(o => o.Owner)
                .Include(o => o.Members)
                .ThenInclude(m => m.User)
                .Include(o => o.Teams)
                .ThenInclude(t => t.TeamMembers)
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization with full details for ID {OrganizationId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization with full details.", ex);
        }
    }

    public async Task<IEnumerable<Organization>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.OwnerId == ownerId && o.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organizations for owner ID {OwnerId}.", ownerId);
            throw new DatabaseException("An error occurred while retrieving organizations by owner.", ex);
        }
    }

    public async Task<IEnumerable<Organization>> GetActiveOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.IsActive && o.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active organizations.");
            throw new DatabaseException("An error occurred while retrieving active organizations.", ex);
        }
    }

    public async Task<IEnumerable<Organization>> GetSubscribedOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.IsSubscribed && o.DeletedAt == null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving subscribed organizations.");
            throw new DatabaseException("An error occurred while retrieving subscribed organizations.", ex);
        }
    }

    public async Task<Organization?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Name == name && o.DeletedAt == null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization with name {OrganizationName}.", name);
            throw new DatabaseException("An error occurred while retrieving the organization by name.", ex);
        }
    }

    public async Task<bool> IsOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .AnyAsync(o => o.Id == organizationId && o.OwnerId == userId && o.DeletedAt == null,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user {UserId} is owner of organization {OrganizationId}.", userId, organizationId);
            throw new DatabaseException("An error occurred while checking organization ownership.", ex);
        }
    }

    public async Task<int> GetMemberCountAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Set<OrganizationMember>()
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting member count for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while getting the member count.", ex);
        }
    }

    public async Task<IEnumerable<Organization>> SearchByNameAsync(string searchTerm, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Name.Contains(searchTerm) && o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching organizations with term {SearchTerm}, page {Page}, pageSize {PageSize}.", searchTerm, page, pageSize);
            throw new DatabaseException("An error occurred while searching organizations.", ex);
        }
    }
    
    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == id)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.DeletedAt, (DateTime?)null)
                        .SetProperty(o => o.IsActive, true)
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error restoring organization with ID {OrganizationId}.", id);
            throw new DatabaseException("An error occurred while restoring the organization.", ex);
        }
    }


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