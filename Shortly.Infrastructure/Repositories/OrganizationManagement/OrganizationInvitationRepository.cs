using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.OrganizationManagement;

/// <summary>
/// Repository implementation for managing OrganizationInvitation entities in the database.
/// Provides CRUD operations, invitation management, status updates, and organization invitation-specific business logic
/// with comprehensive error handling and logging.
/// </summary>
/// <param name="dbContext">The Entity Framework database context for SQL Server operations.</param>
/// <param name="logger">The logger instance for recording operation details and errors.</param>
public class OrganizationInvitationRepository(
    SQLServerDbContext dbContext,
    ILogger<OrganizationInvitationRepository> logger) : IOrganizationInvitationRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationInvitation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all organization invitations.");
            throw new DatabaseException("An error occurred while retrieving organization invitations.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving organization invitation with ID {InvitationId}.", id);
            throw new DatabaseException("An error occurred while retrieving the organization invitation.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationInvitation>> GetByOrganizationIdAsync(Guid organizationId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Include(i => i.InvitedByMember)
                .Where(i => i.OrganizationId == organizationId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving invitations for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while retrieving organization invitations.", ex);
        }
    }
    
    /// <inheritdoc />
    public async Task<OrganizationInvitation?> GetByEmailAndOrganizationAsync(string email, Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvitedUserEmail == email && i.OrganizationId == organizationId,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving invitation for email {Email} in organization {OrganizationId}.", 
                email, organizationId);
            throw new DatabaseException("An error occurred while retrieving the invitation by email and organization.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationInvitation>> GetPendingInvitationsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => i.OrganizationId == organizationId && i.Status == enInvitationStatus.Pending)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving pending invitations for organization {OrganizationId}.", organizationId);
            throw new DatabaseException("An error occurred while retrieving pending invitations.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationInvitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => (i.Status == enInvitationStatus.Pending ||
                             i.Status == enInvitationStatus.EmailSent) &&
                            i.ExpiresAt < DateTime.UtcNow)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving expired invitations.");
            throw new DatabaseException("An error occurred while retrieving expired invitations.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationInvitation> AddAsync(OrganizationInvitation entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.OrganizationInvitations.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding organization invitation for email {Email} to organization {OrganizationId}.", 
                entity.InvitedUserEmail, entity.OrganizationId);
            throw new DatabaseException("An error occurred while adding the organization invitation.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(OrganizationInvitation entity, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.OrganizationInvitations.Update(entity);
            return await dbContext.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating organization invitation with ID {InvitationId}.", entity.Id);
            throw new DatabaseException("An error occurred while updating the organization invitation.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => i.Id == id)
                .ExecuteDeleteAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting organization invitation with ID {InvitationId}.", id);
            throw new DatabaseException("An error occurred while deleting the organization invitation.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .AnyAsync(i => i.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if organization invitation with ID {InvitationId} exists.", id);
            throw new DatabaseException("An error occurred while checking invitation existence.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasPendingInvitationAsync(string email, Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .AnyAsync(
                    i => i.InvitedUserEmail == email &&
                         i.OrganizationId == organizationId &&
                         i.Status == enInvitationStatus.Pending &&
                        !i.IsExpired
                    , cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking for pending invitation for email {Email} in organization {OrganizationId}.", 
                email, organizationId);
            throw new DatabaseException("An error occurred while checking for pending invitations.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExpireInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => i.Id == invitationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.IsExpired, true)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error expiring invitation with ID {InvitationId}.", invitationId);
            throw new DatabaseException("An error occurred while expiring the invitation.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> AcceptInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => i.Id == invitationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.Status, enInvitationStatus.Registered)
                        .SetProperty(i => i.RegisteredAt, DateTime.UtcNow)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error accepting invitation with ID {InvitationId}.", invitationId);
            throw new DatabaseException("An error occurred while accepting the invitation.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RejectInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => i.Id == invitationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.Status, enInvitationStatus.Rejected)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rejecting invitation with ID {InvitationId}.", invitationId);
            throw new DatabaseException("An error occurred while rejecting the invitation.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkInvitationAsSentAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => i.Id == invitationId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.Status, enInvitationStatus.EmailSent)
                    , cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking invitation as sent with ID {InvitationId}.", invitationId);
            throw new DatabaseException("An error occurred while marking invitation as sent.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> CleanupExpiredInvitationsAsync()
    {
        try
        {
            return await dbContext.OrganizationInvitations
                .AsNoTracking()
                .Where(i => i.Status == enInvitationStatus.Pending && i.ExpiresAt < DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.IsExpired, true)) > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up expired invitations.");
            throw new DatabaseException("An error occurred while cleaning up expired invitations.", ex);
        }
    }
}