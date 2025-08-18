using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

/// <summary>
/// Repository implementation for managing OrganizationMember entities in the database.
/// Provides CRUD operations, membership queries, and organization-member-specific business logic
/// with comprehensive error handling and logging.
/// </summary>
/// <param name="dbContext">The Entity Framework database context for SQL Server operations.</param>
/// <param name="logger">The logger instance for recording operation details and errors.</param>
public class OrganizationMemberRepository(SQLServerDbContext dbContext, ILogger<OrganizationMember> logger) : IOrganizationMemberRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMember>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.IsActive)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all organization members.");
            throw new DatabaseException("An error occurred while retrieving organization members.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization member with ID {MemberId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization member.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMember>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .Include(m => m.Organization)
                .Include(m => m.Role)
                .Where(m => m.OrganizationId == organizationId && m.IsActive)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization members for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while retrieving organization members.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .Include(m => m.Organization)
                .Where(m => m.UserId == userId && m.IsActive)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization memberships for user {UserId}.", userId);
            throw new DatabaseException("An error occurred while retrieving user organization memberships.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMember>> GetActiveMembers(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.OrganizationId == organizationId && m.IsActive)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active members for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while retrieving active organization members.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationMember>> GetMembersByRoleAsync(Guid organizationId, enUserRole roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .Include(m => m.User)
                .Where(m => m.OrganizationId == organizationId && m.RoleId == roleId && m.IsActive)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving members with role {RoleId} for organization {OrganizationId}.", 
                roleId, organizationId);
            throw new DatabaseException("An error occurred while retrieving members by role.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationMember?> GetByOrganizationAndUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId && m.IsActive,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization member for organization {OrganizationId} and user {UserId}.", 
                organizationId, userId);
            throw new DatabaseException("An error occurred while retrieving the organization member.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<int> GetMemberCountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .CountAsync(m => m.OrganizationId == organizationId && m.IsActive, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting member count for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while getting the member count.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<OrganizationMember?> GetMemberWithRoleAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .Include(m => m.User)
                .Include(m => m.Role)
                .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId && m.IsActive,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving member with role for user {UserId} in organization {OrganizationId}.", 
                userId, organizationId);
            throw new DatabaseException("An error occurred while retrieving the member with role details.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<OrganizationMember> AddAsync(OrganizationMember entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.OrganizationMembers.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding organization member for user {UserId} to organization {OrganizationId}.", 
                entity.UserId, entity.OrganizationId);
            throw new DatabaseException("An error occurred while adding the organization member.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(OrganizationMember entity, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.OrganizationMembers.Update(entity);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating organization member with ID {MemberId}.", entity.Id);
            throw new DatabaseException("An error occurred while updating the organization member.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.Id == id)
                .ExecuteDeleteAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting organization member with ID {MemberId}.", id);
            throw new DatabaseException("An error occurred while deleting the organization member.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(m => m.IsActive, false)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing user {UserId} from organization {OrganizationId}.", 
                userId, organizationId);
            throw new DatabaseException("An error occurred while removing the organization member.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .AnyAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if organization member with ID {MemberId} exists.", id);
            throw new DatabaseException("An error occurred while checking organization member existence.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationMembers
                .AsNoTracking()
                .AnyAsync(m => m.UserId == userId && m.OrganizationId == organizationId && m.IsActive,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user {UserId} is member of organization {OrganizationId}.", 
                userId, organizationId);
            throw new DatabaseException("An error occurred while checking organization membership.", ex);
        }
    }
}