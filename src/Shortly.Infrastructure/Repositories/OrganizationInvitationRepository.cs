using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Enums;
using Shortly.Domain.Invitations;

namespace Shortly.Infrastructure.Repositories;

internal sealed class OrganizationInvitationRepository(ApplicationDbContext dbContext)
    : Repository<OrganizationInvitation>(dbContext), IOrganizationInvitationRepository
{
    public async Task<OrganizationInvitation?> GetByTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<OrganizationInvitation>()
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> HasPendingInvitationAsync(string email, Guid organizationId, DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<OrganizationInvitation>()
            .AsNoTracking()
            .AnyAsync(i => i.InvitedUserEmail == email && i.OrganizationId == organizationId &&
                           (i.Status == enInvitationStatus.Pending || i.Status == enInvitationStatus.EmailSent) &&
                           i.ExpiresAt >= utcNow, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationInvitation>> GetExpiredInvitationsAsync(DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<OrganizationInvitation>()
            .AsNoTracking()
            .Where(i => i.ExpiresAt < utcNow &&
                        (i.Status == enInvitationStatus.Pending || i.Status == enInvitationStatus.EmailSent))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CleanupExpiredInvitations(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<OrganizationInvitation>()
            .Where(i => i.ExpiresAt < utcNow &&
                        (i.Status == enInvitationStatus.Pending || i.Status == enInvitationStatus.EmailSent))
            .ExecuteDeleteAsync(cancellationToken);
    }
}