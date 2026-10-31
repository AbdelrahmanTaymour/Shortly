namespace Shortly.Domain.Members;

public interface IOrganizationMemberRepository
{
    Task<OrganizationMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetByOrganizationAndUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    void Add(OrganizationMember member);
}