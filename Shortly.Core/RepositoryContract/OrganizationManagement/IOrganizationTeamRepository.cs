using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

public interface IOrganizationTeamRepository
{
    Task<IEnumerable<OrganizationTeam>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationTeam?> GetByIdAsync(Guid id);
    Task<IEnumerable<OrganizationTeam>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationTeam>> GetManagedTeamsAsync(Guid managerId, CancellationToken cancellationToken = default);
    Task<OrganizationTeam?> GetByIdWithMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<OrganizationTeam?> GetByNameAndOrganizationAsync(string name, Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> GetTeamMemberCountAsync(Guid teamId, CancellationToken cancellationToken = default); 
    Task<OrganizationTeam> AddAsync(OrganizationTeam entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(OrganizationTeam entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsTeamManagerAsync(Guid teamId, Guid managerId);
}