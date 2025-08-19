using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

public class OrganizationTeamMemberRepository(
    SQLServerDbContext dbContext,
    ILogger<OrganizationTeamMemberRepository> logger) : IOrganizationTeamMemberRepository
{
    public async Task<IEnumerable<OrganizationTeamMember>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all organization team members.");
            throw new DatabaseException("An error occurred while retrieving organization team members.", ex);
        }
    }

    public async Task<OrganizationTeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(tm => tm.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization team member with ID {TeamMemberId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization team member.", ex);
        }
    }

    public async Task<IEnumerable<OrganizationTeamMember>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .Include(tm => tm.Member)
                .ThenInclude(m => m.User)
                .Where(tm => tm.TeamId == teamId)
                .OrderBy(tm => tm.JoinedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving team members for team {TeamId}.", teamId);
            throw new DatabaseException("An error occurred while retrieving team members.", ex);
        }
    }

    public async Task<IEnumerable<OrganizationTeamMember>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .Include(tm => tm.Team)
                .Where(tm => tm.MemberId == memberId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving team memberships for member {MemberId}.", memberId);
            throw new DatabaseException("An error occurred while retrieving member team memberships.", ex);
        }
    }

    public async Task<OrganizationTeamMember?> GetByTeamAndMemberAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.MemberId == memberId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving team membership for team {TeamId} and member {MemberId}.", 
                teamId, memberId);
            throw new DatabaseException("An error occurred while retrieving the team membership.", ex);
        }
    }

    public async Task<IEnumerable<OrganizationTeam>> GetTeamsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .Include(tm => tm.Team)
                .Where(tm => tm.MemberId == memberId)
                .Select(tm => tm.Team)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving teams for member {MemberId}.", memberId);
            throw new DatabaseException("An error occurred while retrieving member teams.", ex);
        }
    }

    public async Task<OrganizationTeamMember> AddAsync(OrganizationTeamMember entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.OrganizationTeamMembers.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding member {MemberId} to team {TeamId}.", 
                entity.MemberId, entity.TeamId);
            throw new DatabaseException("An error occurred while adding the organization team member.", ex);
        }
    }

    public async Task<bool> UpdateAsync(OrganizationTeamMember entity, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.OrganizationTeamMembers.Update(entity);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating organization team member with ID {TeamMemberId}.", entity.Id);
            throw new DatabaseException("An error occurred while updating the organization team member.", ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .Where(tm => tm.Id == id)
                .ExecuteDeleteAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting organization team member with ID {TeamMemberId}.", id);
            throw new DatabaseException("An error occurred while deleting the organization team member.", ex);
        }
    }

    public async Task<bool> RemoveFromTeamAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .Where(tm => tm.TeamId == teamId && tm.MemberId == memberId)
                .ExecuteDeleteAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing member {MemberId} from team {TeamId}.", memberId, teamId);
            throw new DatabaseException("An error occurred while removing the member from the team.", ex);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .AnyAsync(tm => tm.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if organization team member with ID {TeamMemberId} exists.", id);
            throw new DatabaseException("An error occurred while checking team member existence.", ex);
        }
    }

    public async Task<bool> IsMemberOfTeamAsync(Guid memberId, Guid teamId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationTeamMembers
                .AsNoTracking()
                .AnyAsync(tm => tm.MemberId == memberId && tm.TeamId == teamId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if member {MemberId} belongs to team {TeamId}.", 
                memberId, teamId);
            throw new DatabaseException("An error occurred while checking team membership.", ex);
        }
    }
}