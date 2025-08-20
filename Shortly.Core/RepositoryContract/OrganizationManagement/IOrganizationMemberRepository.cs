using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

/// <summary>
/// Defins repository for managing OrganizationMember entities in the database.
/// Provides CRUD operations, membership queries, and organization-member-specific business logic.
/// </summary>
public interface IOrganizationMemberRepository
{
    /// <summary>
    /// Retrieves all active organization members from the database.
    /// </summary>
    /// <param name="page">The page number for pagination (starting from 1).</param>
    /// <param name="pageSize">The number of organizations per page.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of all active organization members.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationMember>> GetAllAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves an organization member by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the organization member.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization member if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Retrieves an active organization member by organization and user identifiers.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization member if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationMember?> GetByOrganizationAndUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all active members of a specific organization, including organization and role details.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organization members with organization and role details.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationMember>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all active organization memberships for a specific user, including organization details.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organization memberships with organization details.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationMember>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all active members of a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of active organization members.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationMember>> GetActiveMembers(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all active members of a specific organization with a specific role including user details.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="roleId">The role identifier to filter members by.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organization members with the specified role and user details.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationMember>> GetMembersByRoleAsync(Guid organizationId, enUserRole roleId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Gets the total count of active members in a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The number of active members in the organization.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<int> GetMemberCountByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves an active organization member with user and role details.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization member with user and role details if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationMember?> GetMemberWithRoleAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Adds a new organization member to the database.
    /// </summary>
    /// <param name="entity">The organization member to add.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The added organization member with any database-generated values.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationMember> AddAsync(OrganizationMember entity, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Updates an existing organization member in the database.
    /// </summary>
    /// <param name="entity">The organization member with updated values.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> UpdateAsync(OrganizationMember entity, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Updates the role of a specific organization member.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the member whose role is being updated.</param>
    /// <param name="newRoleId">The new role to assign to the member.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during the update operation.</exception>
    Task<bool> UpdateMemberRoleAsync(Guid organizationId, Guid userId, enUserRole newRoleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the custom permissions of a specific organization member.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the member whose permissions are being updated.</param>
    /// <param name="permissions">The new set of permissions to assign to the member.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during the update operation.</exception>
    Task<bool> UpdateMemberPermissionsAsync(Guid organizationId, Guid userId, enPermissions permissions, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Permanently removes a member from an organization by hard deleting the membership record.
    /// </summary>
    /// <param name="id">The unique identifier of the organization member to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the member was successfully removed; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes an organization member by setting their IsActive status to false.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user to remove.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the member was successfully removed; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> RemoveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Checks if an organization member exists in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the organization member to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization member exists; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Checks if a user is an active member of a specific organization.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the user is an active member of the organization; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> IsMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
    
}