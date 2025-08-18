using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

/// <summary>
/// Defins repository for managing Organization entities in the database.
/// Provides CRUD operations, querying capabilities, and organization-specific business logic
/// with comprehensive error handling and logging.
/// </summary>
public interface IOrganizationRepository
{
    /// <summary>
    /// Retrieves all non-deleted organizations from the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of all active organizations.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<Organization>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an organization by its unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<Organization?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new organization to the database.
    /// </summary>
    /// <param name="organization">The organization to add.</param>
    /// <returns>The added organization with any database-generated values.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<Organization> AddAsync(Organization organization);
    
    /// <summary>
    /// Updates an existing organization in the database.
    /// </summary>
    /// <param name="organization">The organization with updated values.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> UpdateAsync(Organization organization, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes an organization by setting its DeletedAt timestamp and IsActive status.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> DeleteAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an organization exists in the database, including soft-deleted organizations.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization exists; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> ExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization by its unique identifier, including its members and associated user details.
    /// </summary>
    /// <param name="id">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization with members if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<Organization?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves an organization by its unique identifier, including its teams and team members.
    /// </summary>
    /// <param name="id">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization with teams if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<Organization?> GetByIdWithTeamsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization by its unique identifier including all related data (owner, members, teams).
    /// </summary>
    /// <param name="id">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization with full details if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<Organization?> GetByIdWithFullDetailsAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all organizations owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The unique identifier of the owner.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organizations owned by the specified user.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<Organization>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all active organizations from the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of active organizations.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<Organization>> GetActiveOrganizationsAsync(CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all subscribed organizations from the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of subscribed organizations.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<Organization>> GetSubscribedOrganizationsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization by its name.
    /// </summary>
    /// <param name="name">The name of the organization to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<Organization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Checks if a user is the owner of a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the user is the owner of the organization; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> IsOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Gets the total count of members in a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The number of members in the organization.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<int> GetMemberCountAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches for organizations by name with pagination support.
    /// </summary>
    /// <param name="searchTerm">The search term to match against organization names.</param>
    /// <param name="page">The page number for pagination (starting from 1).</param>
    /// <param name="pageSize">The number of organizations per page.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A paginated collection of organizations matching the search term.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<Organization>> SearchByNameAsync(string searchTerm, int page = 0, int pageSize = 50, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Restores a soft-deleted organization by clearing its DeletedAt timestamp and setting it as active.
    /// </summary>
    /// <param name="id">The unique identifier of the organization to restore.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the restoration was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
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