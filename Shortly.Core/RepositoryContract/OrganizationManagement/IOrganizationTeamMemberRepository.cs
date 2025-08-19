using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

public interface IOrganizationTeamMemberRepository
{
    Task<IEnumerable<OrganizationTeamMember>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationTeamMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationTeamMember>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationTeamMember>> GetByMemberIdAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<OrganizationTeamMember?> GetByTeamAndMemberAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationTeam>> GetTeamsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default);
    Task<OrganizationTeamMember> AddAsync(OrganizationTeamMember entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(OrganizationTeamMember entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromTeamAsync(Guid teamId, Guid memberId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfTeamAsync(Guid memberId, Guid teamId, CancellationToken cancellationToken = default);
}