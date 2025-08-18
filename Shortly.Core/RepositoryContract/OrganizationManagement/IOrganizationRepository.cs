using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

public interface IOrganizationRepository
{
    Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Organization?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task<Organization> AddAsync(Organization organization);
    
    Task<bool> UpdateAsync(Organization organization, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    Task<Organization?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Organization?> GetByIdWithTeamsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Organization?> GetByIdWithFullDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetActiveOrganizationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetSubscribedOrganizationsAsync(CancellationToken cancellationToken = default);
    Task<Organization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> IsOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> SearchByNameAsync(string searchTerm, int page = 0, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines whether the specified user is the owner of any organization.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check.</param>
    /// <returns>
    /// <c>true</c> if the user owns at least one organization; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="DatabaseException">
    /// Thrown when an error occurs while accessing the database.
    /// </exception>
    Task<bool> IsUserOwnerOfAnyOrganization(Guid userId);
}