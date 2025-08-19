using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

public interface IOrganizationInvitationRepository
{
    Task<IEnumerable<OrganizationInvitation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationInvitation>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<OrganizationInvitation?> GetByEmailAndOrganizationAsync(string email, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationInvitation>> GetPendingInvitationsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationInvitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default);
    Task<OrganizationInvitation> AddAsync(OrganizationInvitation entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(OrganizationInvitation entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInvitationAsync(string email, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> ExpireInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<bool> AcceptInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<bool> RejectInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<bool> CleanupExpiredInvitationsAsync();
}