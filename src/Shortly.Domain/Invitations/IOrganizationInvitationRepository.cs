namespace Shortly.Domain.Invitations;

public interface IOrganizationInvitationRepository
{
    Task<OrganizationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInvitationAsync(string email, Guid organizationId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Marks every pending invitation that reached its expiration as expired, and returns how many changed.</summary>
    Task<int> MarkOverdueInvitationsExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    void Add(OrganizationInvitation invitation);
}