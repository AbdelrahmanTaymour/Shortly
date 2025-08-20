using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

public interface IOrganizationMemberService
{
    Task<IEnumerable<OrganizationMember>> GetAllMembersAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationMember>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrganizationMember>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetMembershipAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<OrganizationMember> AddMemberAsync(Guid organizationId, Guid userId, enUserRole roleId, Guid invitedBy, CancellationToken cancellationToken = default);
    Task<bool> RemoveMemberAsync(Guid organizationId, Guid userId, Guid requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMemberRoleAsync(Guid organizationId, Guid userId, enUserRole newRoleId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMemberPermissionsAsync(Guid organizationId, Guid userId, enPermissions permissions, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
}