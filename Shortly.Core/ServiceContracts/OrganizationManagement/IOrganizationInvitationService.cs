using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

public interface IOrganizationInvitationService
{
    Task<OrganizationInvitation> CreateInvitationAsync(Guid organizationId, InviteMemberDto dto, CancellationToken cancellationToken = default);
    Task<OrganizationInvitation?> GetInvitationByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationInvitation>> GetOrganizationInvitationsAsync(Guid organizationId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<bool> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RejectInvitationAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> CancelInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> ResendInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> CleanupExpiredInvitationsAsync();
    Task<bool> ValidateInvitationTokenAsync(string token, CancellationToken cancellationToken = default);
}