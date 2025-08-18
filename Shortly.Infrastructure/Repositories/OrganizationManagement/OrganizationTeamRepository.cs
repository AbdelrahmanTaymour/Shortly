using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

/// <summary>
/// Repository implementation for managing OrganizationTeam entities in the database.
/// Provides CRUD operations, team queries, and organization team-specific business logic
/// with comprehensive error handling and logging.
/// </summary>
/// <param name="dbContext">The Entity Framework database context for SQL Server operations.</param>
/// <param name="logger">The logger instance for recording operation details and errors.</param>
public class OrganizationTeamRepository(SQLServerDbContext dbContext, ILogger<OrganizationMemberRepository> logger) : IOrganizationTeamRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationTeam>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all organization teams.");
            throw new DatabaseException("An error occurred while retrieving organization teams.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<OrganizationTeam?> GetByIdAsync(Guid id)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .FindAsync(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization team with ID {TeamId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization team.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationTeam>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .Include(t => t.TeamManager)
                .Where(t => t.OrganizationId == organizationId)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving teams for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while retrieving organization teams.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationTeam>> GetManagedTeamsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .Where(t => t.TeamManagerId == managerId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving teams managed by manager {ManagerId}.", managerId);
            throw new DatabaseException("An error occurred while retrieving managed teams.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationTeam?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .Include(t => t.TeamMembers)
                    .ThenInclude(tm => tm.Member)
                         .ThenInclude(m => m.User)
                .Include(t => t.TeamManager)
                .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving team with members for team ID {TeamId}.", teamId);
            throw new DatabaseException("An error occurred while retrieving the team with members.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationTeam?> GetByNameAndOrganizationAsync(string name, Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == name && t.OrganizationId == organizationId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving team with name {TeamName} in organization {OrganizationId}.", 
                name, organizationId);
            throw new DatabaseException("An error occurred while retrieving the team by name.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> GetTeamMemberCountAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Set<OrganizationTeamMember>()
                .AsNoTracking()
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting member count for team {TeamId}.", teamId);
            throw new DatabaseException("An error occurred while getting the team member count.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationTeam> AddAsync(OrganizationTeam entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.OrganizationTeams.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding organization team with name {TeamName} to organization {OrganizationId}.", 
                entity.Name, entity.OrganizationId);
            throw new DatabaseException("An error occurred while adding the organization team.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(OrganizationTeam entity, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.OrganizationTeams.Update(entity);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating organization team with ID {TeamId}.", entity.Id);
            throw new DatabaseException("An error occurred while updating the organization team.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .Where(t => t.Id == id)
                .ExecuteDeleteAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting organization team with ID {TeamId}.", id);
            throw new DatabaseException("An error occurred while deleting the organization team.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .AnyAsync(t => t.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if organization team with ID {TeamId} exists.", id);
            throw new DatabaseException("An error occurred while checking team existence.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsTeamManagerAsync(Guid teamId, Guid managerId)
    {
        try
        {
            return await dbContext.OrganizationTeams
                .AsNoTracking()
                .AnyAsync(t => t.Id == teamId && t.TeamManagerId == managerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if user {ManagerId} is manager of team {TeamId}.", 
                managerId, teamId);
            throw new DatabaseException("An error occurred while checking team management.", ex);
        }
    }
}