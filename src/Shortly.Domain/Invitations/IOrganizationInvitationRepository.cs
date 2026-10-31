namespace Shortly.Domain.Invitations;

public interface IOrganizationInvitationRepository
{
    Task<OrganizationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInvitationAsync(string email, Guid organizationId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationInvitation>> GetExpiredInvitationsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredInvitations(DateTime utcNow, CancellationToken cancellationToken = default);
    void Add(OrganizationInvitation invitation);
    void Remove(OrganizationInvitation invitation);
}