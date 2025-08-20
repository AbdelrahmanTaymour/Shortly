using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

public interface IOrganizationTeamService
{
    Task<OrganizationTeam?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<OrganizationTeam?> GetTeamWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationTeam>> GetOrganizationTeamsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationTeamMember>> GetTeamMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<OrganizationTeam> CreateTeamAsync(CreateTeamDto dto);
    Task<bool> AddMemberToTeamAsync(Guid teamId, Guid memberId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateTeamAsync(Guid teamId, string? name, string? description, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteTeamAsync(Guid teamId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberFromTeamAsync(Guid teamId, Guid memberId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> ChangeTeamManagerAsync(Guid teamId, Guid newManagerId, Guid requestingUserId, CancellationToken cancellationToken = default);
}