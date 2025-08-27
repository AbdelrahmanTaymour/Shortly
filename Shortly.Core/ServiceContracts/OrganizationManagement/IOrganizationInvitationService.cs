using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.OrganizationManagement;

/// <summary>
/// Defins operations for managing organization invitations,
/// including creation, retrieval, acceptance, rejection, cancellation,
/// resending, cleanup, and validation.
/// </summary>
public interface IOrganizationInvitationService
{
    /// <summary>
    /// Creates a new invitation for a user to join an organization.
    /// </summary>
    /// <param name="organizationId">The identifier of the target organization.</param>
    /// <param name="dto">The invitation details containing the invitee email and inviter information.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>The created <see cref="OrganizationInvitation"/> instance.</returns>
    /// <exception cref="NotFoundException">Thrown if the organization is not found.</exception>
    /// <exception cref="BusinessRuleException">Thrown if the organization has reached its member limit.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the user already has a pending invitation.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationInvitationDto> CreateInvitationAsync(Guid organizationId, InviteMemberDto dto, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Retrieves an invitation by its unique token.
    /// </summary>
    /// <param name="id">The invitation token Id.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>The corresponding <see cref="OrganizationInvitation"/> if found.</returns>
    /// <exception cref="NotFoundException">Thrown if the invitation is not found.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<OrganizationInvitationDto> GetInvitationByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all invitations for a given organization with pagination support.
    /// </summary>
    /// <param name="organizationId">The identifier of the organization.</param>
    /// <param name="page">The page number (default is 1).</param>
    /// <param name="pageSize">The number of results per page (default is 10).</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>A collection of <see cref="OrganizationInvitation"/> records.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<IEnumerable<OrganizationInvitationDto>> GetOrganizationInvitationsAsync(Guid organizationId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Accepts an invitation using its token and adds the user as a member of the organization.
    /// </summary>
    /// <param name="token">The invitation token.</param>
    /// <param name="userId">The identifier of the user accepting the invitation.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns><c>true</c> if the invitation is accepted successfully.</returns>
    /// <exception cref="ValidationException">Thrown if the invitation is invalid, expired, or already handled.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rejects an invitation using its token.
    /// </summary>
    /// <param name="token">The encrypted identifier of the invitation.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns><c>true</c> if the invitation is rejected successfully.</returns>
    /// <exception cref="ValidationException">Thrown if the invitation is invalid, expired, or already handled.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> RejectInvitationAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels an invitation if the requesting user is the inviter.
    /// </summary>
    /// <param name="invitationId">The identifier of the invitation (InvitationId of the invitation Token).</param>
    /// <param name="requestingUserId">The identifier of the user requesting cancellation.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns><c>true</c> if the invitation is canceled successfully.</returns>
    /// <exception cref="NotFoundException">Thrown if the invitation is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown if a user other than the inviter attempts to cancel the invitation.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> CancelInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Resends an existing invitation by regenerating its token and extending its expiration date.
    /// </summary>
    /// <param name="invitationId">The identifier of the invitation.</param>
    /// <param name="requestingUserId">The identifier of the user requesting to resend the invitation.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns><c>true</c> if the invitation is resent successfully.</returns>
    /// <exception cref="ValidationException">Thrown if the invitation is invalid or not in a state eligible for resending.</exception>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> ResendInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Removes expired invitations from the system.
    /// </summary>
    /// <returns><c>true</c> if the cleanup operation succeeds.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> CleanupExpiredInvitationsAsync();
  
    /// <summary>
    /// Validates whether an invitation token is still active and usable.
    /// </summary>
    /// <param name="token">The encrypted identifier of the invitation.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns><c>true</c> if the token is valid and the invitation is active; otherwise, <c>false</c>.</returns>
    /// <exception cref="DatabaseException">Thrown when a database error occurs during retrieval.</exception>
    Task<bool> ValidateInvitationTokenAsync(string token, CancellationToken cancellationToken = default);
}