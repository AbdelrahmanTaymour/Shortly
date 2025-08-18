using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

public interface IOrganizationMemberRepository
{
    Task<IEnumerable<OrganizationMember>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetByOrganizationAndUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationMember>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationMember>> GetActiveMembers(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationMember>> GetMembersByRoleAsync(Guid organizationId, enUserRole roleId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetMemberWithRoleAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<OrganizationMember> AddAsync(OrganizationMember entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(OrganizationMember entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
    
}