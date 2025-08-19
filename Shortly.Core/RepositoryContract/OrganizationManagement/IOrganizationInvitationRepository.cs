using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract.OrganizationManagement;

/// <summary>
/// Defines repository implementation for managing OrganizationInvitation entities in the database.
/// Provides CRUD operations, invitation management, status updates, and organization invitation-specific business logic.
/// </summary>
public interface IOrganizationInvitationRepository
{
    /// <summary>
    /// Retrieves all organization invitations from the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of all organization invitations.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationInvitation>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization invitation by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the organization invitation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization invitation if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationInvitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves all invitations for a specific organization, including inviter details, ordered by creation date.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of organization invitations with inviter details ordered by creation date (newest first).</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationInvitation>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves an organization invitation by its unique invitation token including organization details.
    /// </summary>
    /// <param name="token">The unique invitation token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization invitation with organization details if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves an organization invitation by email address and organization identifier.
    /// </summary>
    /// <param name="email">The email address of the invited user.</param>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The organization invitation if found; otherwise, null.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationInvitation?> GetByEmailAndOrganizationAsync(string email, Guid organizationId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Retrieves all pending invitations for a specific organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of pending organization invitations.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationInvitation>> GetPendingInvitationsAsync(Guid organizationId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Retrieves all expired invitations that are still pending or have been sent via email.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of expired organization invitations.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<IEnumerable<OrganizationInvitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Adds a new organization invitation to the database.
    /// </summary>
    /// <param name="entity">The organization invitation to add.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The added organization invitation with any database-generated values.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<OrganizationInvitation> AddAsync(OrganizationInvitation entity, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Updates an existing organization invitation in the database.
    /// </summary>
    /// <param name="entity">The organization invitation with updated values.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> UpdateAsync(OrganizationInvitation entity, CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Permanently deletes an organization invitation from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the organization invitation to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Checks if an organization invitation exists in the database.
    /// </summary>
    /// <param name="id">The unique identifier of the organization invitation to check.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the organization invitation exists; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Checks if there is a pending, non-expired invitation for a specific email and organization.
    /// </summary>
    /// <param name="email">The email address to check for pending invitations.</param>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if there is a pending, non-expired invitation; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> HasPendingInvitationAsync(string email, Guid organizationId, CancellationToken cancellationToken = default);
 
    /// <summary>
    /// Marks an organization invitation as expired by setting the IsExpired flag.
    /// </summary>
    /// <param name="invitationId">The unique identifier of the invitation to expire.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the invitation was successfully expired; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> ExpireInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Accepts an organization invitation by updating its status to Registered and setting the registration timestamp.
    /// </summary>
    /// <param name="invitationId">The unique identifier of the invitation to accept.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the invitation was successfully accepted; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> AcceptInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
  
    /// <summary>
    /// Rejects an organization invitation by updating its status to Rejected.
    /// </summary>
    /// <param name="invitationId">The unique identifier of the invitation to reject.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>True if the invitation was successfully rejected; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> RejectInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs cleanup of expired invitations by marking all pending invitations past their expiration date as expired.
    /// </summary>
    /// <returns>True if any invitations were marked as expired; otherwise, false.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs.</exception>
    Task<bool> CleanupExpiredInvitationsAsync();
}