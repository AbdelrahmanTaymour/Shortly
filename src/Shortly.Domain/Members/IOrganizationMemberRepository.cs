namespace Shortly.Domain.Members;

/// <summary>Defines data access operations for organization memberships.</summary>
public interface IOrganizationMemberRepository
{
    Task<OrganizationMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OrganizationMember?> GetByOrganizationAndUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the membership only if it is active AND its parent organization is active and not deleted.
    /// Use this for authorization checks to ensure inactive organizations grant no access.
    /// </summary>
    Task<OrganizationMember?> GetActiveByOrganizationAndUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Verifies whether a user is an active member of an active, non-deleted organization.</summary>
    Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Gets the total count of members belonging to an organization.</summary>
    Task<int> GetMemberCountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);

    void Add(OrganizationMember member);
}