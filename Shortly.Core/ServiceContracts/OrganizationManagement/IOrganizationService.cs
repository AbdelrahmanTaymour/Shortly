using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

/// <summary>
/// Defins Service for managing organizations and their lifecycle operations.
/// Provides business logic for organization CRUD operations, ownership management, member access control,
/// activation/deactivation workflows, and organization search functionality with comprehensive validation and logging.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Retrieves a paginated list of all organizations from the system.
    /// </summary>
    /// <param name="page">The page number for pagination (starting from 1).</param>
    /// <param name="pageSize">The number of organizations per page (default: 50).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organizations for the specified page.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<IEnumerable<OrganizationDto>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization by its unique identifier with basic information.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization if found.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization with the specified ID is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationDto?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization by its unique identifier including all related details (owner, members, teams).
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization with full details including an owner, members, and teams.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization with the specified ID is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationDto?> GetOrganizationWithDetailsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all organizations owned by a specific user.
    /// </summary>
    /// <param name="ownerId">The unique identifier of the organization owner.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organizations owned by the specified user.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<IEnumerable<OrganizationDto>> GetUserOrganizationsAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new organization and automatically adds the creator as the first member with an organization owner role.
    /// </summary>
    /// <param name="dto">The data transfer object containing organization creation details.</param>
    /// <param name="ownerId">The unique identifier of the organization owner.</param>
    /// <returns>The created organization with database-generated values.</returns>
    /// <exception cref="NotFoundException">Thrown when the specified owner user ID does not exist.</exception>
    /// <exception cref="ConflictException">Thrown when an organization with the same name already exists for the same owner. </exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during creation.</exception>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Validates that the specified owner exists
    /// 2. Creates the organization with the provided details
    /// 3. Automatically adds the owner as the first organization member with OrgOwner role
    /// 4. Logs the successful creation
    /// </remarks>
    Task<OrganizationDto> CreateOrganizationAsync(CreateOrganizationDto dto, Guid ownerId);
   
    /// <summary>
    /// Updates an existing organization with the provided information. Only non-null values are updated.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to update.</param>
    /// <param name="dto">The data transfer object containing updated organization details.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated organization with the new values.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization with the specified ID is not found.</exception>
    /// <exception cref="ConflictException">Thrown when the owner already has an organization with the same updated name.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an update.</exception>
    /// <remarks>
    /// This method uses partial update semantics - only properties with non-null values in the DTO will be updated.
    /// Properties with null values will remain unchanged in the organization.
    /// </remarks>
    Task<OrganizationDto> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationDto dto, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes an organization. Only the organization owner can perform this operation.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to delete.</param>
    /// <param name="requestingUserId">The unique identifier of the user requesting the deletion.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization was successfully deleted; otherwise, false.</returns>
    /// <exception cref="ForbiddenException">Thrown when the requesting user is not the organization owner.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during deletion.</exception>
    /// <remarks>
    /// This operation performs a soft delete, marking the organization as deleted rather than permanently removing it.
    /// Only the organization owner has permission to delete the organization.
    /// </remarks>
    Task<bool> DeleteOrganizationAsync(Guid organizationId, Guid requestingUserId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Activates an organization that was previously deactivated.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to activate.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization was successfully activated.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization is not found or already activated.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during activation.</exception>
    Task<bool> ActivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Deactivates an active organization, preventing normal operations while preserving data.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization to deactivate.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization was successfully deactivated.</returns>
    /// <exception cref="NotFoundException">Thrown when the organization is not found or already deactivated.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during deactivation.</exception>
    Task<bool> DeactivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Transfers ownership of an organization from the current owner to a new owner.
    /// The new owner must already be a member of the organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="currentOwnerId">The unique identifier of the current organization owner.</param>
    /// <param name="newOwnerId">The unique identifier of the new organization owner.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the ownership was successfully transferred.</returns>
    /// <exception cref="ArgumentException">Thrown when the new owner is not a member of the organization.</exception>
    /// <exception cref="NotFoundException">Thrown when the organization is not found or the current owner doesn't match.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during transfer.</exception>
    /// <remarks>
    /// The new owner must be an existing member of the organization before ownership can be transferred.
    /// This ensures they already have appropriate access and context within the organization.
    /// </remarks>
    Task<bool> TransferOwnershipAsync(Guid organizationId, Guid currentOwnerId, Guid newOwnerId, CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Checks if a user has access to an organization by verifying their membership status.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the user is a member of the organization and can access it; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during an access check.</exception>
    /// <remarks>
    /// This method is typically used for authorization checks to determine if a user should be granted
    /// access to organization-specific resources or operations.
    /// </remarks>
    Task<bool> CanUserAccessOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Searches for organizations by name with pagination support.
    /// </summary>
    /// <param name="searchTerm">The search term to match against organization names.</param>
    /// <param name="page">The page number for pagination (starting from 1).</param>
    /// <param name="pageSize">The number of organizations per page (default: 50).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A paginated collection of organizations matching the search term.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during search.</exception>
    /// <remarks>
    /// The search performs a case-insensitive partial match on organization names.
    /// Results are paginated to improve performance for large result sets.
    /// </remarks>
    Task<IEnumerable<OrganizationDto>> SearchOrganizationsAsync(string searchTerm, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}