namespace Shortly.Domain.Organizations;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    void Add(Organization organization);
}