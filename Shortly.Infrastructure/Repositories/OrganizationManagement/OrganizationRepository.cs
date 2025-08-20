using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

/// <summary>
/// Repository implementation for managing Organization entities in the database.
/// Provides CRUD operations, querying capabilities, and organization-specific business logic
/// with comprehensive error handling and logging.
/// </summary>
/// <param name="dbContext">The Entity Framework database context for SQL Server operations.</param>
/// <param name="logger">The logger instance for recording operation details and errors.</param>
public class OrganizationRepository(SQLServerDbContext dbContext, ILogger<OrganizationRepository> logger)
    : IOrganizationRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Organization>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all organizations.");
            throw new DatabaseException("An error occurred while retrieving organizations.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization with ID {OrganizationId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization.", ex);
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<Organization> UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.Organizations.Update(organization);
            await dbContext.SaveChangesAsync(cancellationToken);
            return organization;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating organization with ID {OrganizationId}.", organization.Id);
            throw new DatabaseException("An error occurred while updating the organization.", ex);
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<bool> TransferOwnershipAsync(Guid organizationId, Guid currentOwnerId, Guid newOwnerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == organizationId && o.OwnerId == currentOwnerId && o.DeletedAt == null)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.OwnerId, newOwnerId)
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error transferring organization's ownership with ID {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while transferring organization's ownership.", ex);
        }
    }

    /// <inheritdoc/>
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
    public async Task<bool> ActivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == organizationId && o.DeletedAt == null && !o.IsActive)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.IsActive, true)
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error activating the organization with ID {organizationId}.", organizationId);
            throw new DatabaseException("An error occurred while activating the organization.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Organizations
                .AsNoTracking()
                .Where(o => o.Id == organizationId && o.DeletedAt == null && o.IsActive)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.IsActive, false)
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating the organization with ID {organizationId}.", organizationId);
            throw new DatabaseException("An error occurred while deactivating the organization.", ex);
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