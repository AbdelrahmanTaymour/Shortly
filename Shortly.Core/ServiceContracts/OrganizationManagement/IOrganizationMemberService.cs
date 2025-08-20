using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

/// <summary>
/// Defins service for managing organization members, including adding, removing, and updating member roles and permissions.
/// </summary>
public interface IOrganizationMemberService
{
    /// <summary>
    /// Retrieves all organization members with pagination support.
    /// </summary>
    /// <param name="page">The page number to retrieve (1-based indexing). Default is 1.</param>
    /// <param name="pageSize">The number of members to retrieve per page. Default is 10.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of organization members for the specified page.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<IEnumerable<OrganizationMember>> GetAllMembersAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Retrieves all members belonging to a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of members in the specified organization.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<IEnumerable<OrganizationMember>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Retrieves all organizations that a specific user is a member of.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>A collection of organization memberships for the specified user.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<IEnumerable<OrganizationMember>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Retrieves a specific membership record for a user in an organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The organization member record if found, otherwise null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<OrganizationMember?> GetMembershipAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Adds a new member to an organization with the specified role.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user to add.</param>
    /// <param name="roleId">The role to assign to the new member.</param>
    /// <param name="invitedBy">The unique identifier of the user who invited this member.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The newly created organization member record.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization does not exist.</exception>
    /// <exception cref="BusinessRuleException">Thrown when the organization has reached its member limit.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the user is already a member of the organization.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<OrganizationMember> AddMemberAsync(Guid organizationId, Guid userId, enUserRole roleId, Guid invitedBy, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Removes a member from an organization. Organization owners cannot be removed.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user to remove.</param>
    /// <param name="requestingUserId">The unique identifier of the user requesting the removal.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the member was successfully removed, false otherwise.</returns>
    /// <exception cref="BusinessRuleException">Thrown when attempting to remove an organization owner.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<bool> RemoveMemberAsync(Guid organizationId, Guid userId, Guid requestingUserId, CancellationToken cancellationToken = default);
  
    
    /// <summary>
    /// Updates the role of an existing organization member.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user whose role is being updated.</param>
    /// <param name="newRoleId">The new role to assign to the member.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the role was successfully updated, false otherwise.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization member is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<bool> UpdateMemberRoleAsync(Guid organizationId, Guid userId, enUserRole newRoleId, CancellationToken cancellationToken = default);
   
    
    /// <summary>
    /// Updates the permissions of an existing organization member.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="userId">The unique identifier of the user whose permissions are being updated.</param>
    /// <param name="permissions">The new permissions to assign to the member.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the permissions were successfully updated, false otherwise.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization member is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<bool> UpdateMemberPermissionsAsync(Guid organizationId, Guid userId, enPermissions permissions, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Checks if a user is a member of a specific organization.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>True if the user is a member of the organization, false otherwise.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    Task<bool> IsMemberAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
}