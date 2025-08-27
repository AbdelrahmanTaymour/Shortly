using Microsoft.AspNetCore.Mvc;
using Shortly.API.Controllers.Base;
using Shortly.Core.DTOs.ExceptionsDTOs;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.ServiceContracts.OrganizationManagement;

namespace Shortly.API.Controllers;

/// <summary>
/// Controller for managing organization invitations, providing invitation creation, acceptance, rejection, and management functionality.
/// </summary>
/// <remarks>
/// This controller handles creating invitations for users to join organizations,
/// accepting and rejecting invitations, and managing the invitation lifecycle.
/// </remarks>
[ApiController]
[Route("api/organization-invitations")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class OrganizationInvitationController(IOrganizationInvitationService invitationService) : ControllerApiBase
{
    /// <summary>
    /// Retrieves all invitations for a specific organization with pagination support.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A paginated list of organization invitations.</returns>
    /// <example>GET /api/organization-invitations/organization/550e8400-e29b-41d4-a716-446655440000?page=1&amp;pageSize=10</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-invitations/organization/550e8400-e29b-41d4-a716-446655440000?page=1&amp;pageSize=10
    /// </remarks>
    /// <response code="200">Returns the list of organization invitations.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read organization invitations.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("organization/{organizationId:guid}", Name = "GetOrganizationInvitations")]
    [ProducesResponseType(typeof(IEnumerable<OrganizationInvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrganizationInvitations(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var invitations = await invitationService.GetOrganizationInvitationsAsync(organizationId, page, pageSize, cancellationToken);
        return Ok(invitations);
    }
    
    /// <summary>
    /// Retrieves an invitation by its token.
    /// </summary>
    /// <param name="id">The invitation token ID.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The invitation details if found.</returns>
    /// <example>GET /api/organization-invitations/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-invitations/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="200">Returns the invitation details.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to read invitations.</response>
    /// <response code="404">Invitation with the specified token was not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("{Id:guid}", Name = "GetInvitationById")]
    [ProducesResponseType(typeof(OrganizationInvitationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationService.GetInvitationByIdAsync(id, cancellationToken);
        return Ok(invitation);
    }
    
    /// <summary>
    /// Validates an invitation token to check if it's valid and not expired.
    /// </summary>
    /// <param name="token">The invitation token to validate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the token is valid, false otherwise.</returns>
    /// <example>GET /api/organization-invitations/validate?token=QCqPym5vuxi185cq1viT9DlvR4fV9P0ZvptE1qC7Pkf2FdFnNznFhWqYwzmOIYEU</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      GET /api/organization-invitations/validate?token=QCqPym5vuxi185cq1viT9DlvR4fV9P0ZvptE1qC7Pkf2FdFnNznFhWqYwzmOIYEU
    /// </remarks>
    /// <response code="200">Returns the validation status.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to validate invitations.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpGet("validate", Name = "ValidateInvitationToken")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateToken([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var isValid = await invitationService.ValidateInvitationTokenAsync(token, cancellationToken);
        return Ok(isValid);
    }

    /// <summary>
    /// Creates a new invitation for a user to join an organization.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the organization.</param>
    /// <param name="inviteMemberDto">The invitation data.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The created invitation details.</returns>
    /// <example>POST /api/organization-invitations/organization/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-invitations/organization/550e8400-e29b-41d4-a716-446655440000
    ///      {
    ///         "email": "user@example.com",
    ///         "invitedBy": "660f9511-f3ac-52e5-b827-557766551111"
    ///      }
    /// </remarks>
    /// <response code="201">Invitation created successfully.</response>
    /// <response code="400">Invalid request data or organization has reached the member limit.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to create invitations.</response>
    /// <response code="404">Organization not found.</response>
    /// <response code="409">User already has a pending invitation.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("organization/{organizationId:guid}", Name = "CreateInvitation")]
    [ProducesResponseType(typeof(OrganizationInvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(Guid organizationId, [FromBody] InviteMemberDto inviteMemberDto, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationService.CreateInvitationAsync(organizationId, inviteMemberDto, cancellationToken);
        return CreatedAtRoute("GetInvitationById", new { id = invitation.Id }, invitation);
    }

    /// <summary>
    /// Accepts an invitation to join an organization.
    /// </summary>
    /// <param name="token">The invitation token.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the acceptance operation.</returns>
    /// <example>POST /api/organization-invitations/accept?token=QCqPym5vuxi185cq1viT9DlvR4fV9P0ZvptE1qC7Pkf2FdFnNznFhWqYwzmOIYEU</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-invitations/accept?token=QCqPym5vuxi185cq1viT9DlvR4fV9P0ZvptE1qC7Pkf2FdFnNznFhWqYwzmOIYEU
    /// </remarks>
    /// <response code="200">Invitation accepted successfully.</response>
    /// <response code="400">Invalid or expired invitation.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to accept invitations.</response>
    /// <response code="404">Invitation not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("accept", Name = "AcceptInvitation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Accept([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await invitationService.AcceptInvitationAsync(token, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Rejects an invitation to join an organization.
    /// </summary>
    /// <param name="token">The invitation token.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the rejection operation.</returns>
    /// <example>POST /api/organization-invitations/reject?token=QCqPym5vuxi185cq1viT9DlvR4fV9P0ZvptE1qC7Pkf2FdFnNznFhWqYwzmOIYEU</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-invitations/reject?token=QCqPym5vuxi185cq1viT9DlvR4fV9P0ZvptE1qC7Pkf2FdFnNznFhWqYwzmOIYEU
    /// </remarks>
    /// <response code="200">Invitation rejected successfully.</response>
    /// <response code="400">Invalid or expired invitation.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to reject invitations.</response>
    /// <response code="404">Invitation not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("reject", Name = "RejectInvitation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reject([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        var result = await invitationService.RejectInvitationAsync(token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancels an invitation. Only the user who created the invitation can cancel it.
    /// </summary>
    /// <param name="id">The unique identifier of the invitation to cancel.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the cancellation operation.</returns>
    /// <example>DELETE /api/organization-invitations/550e8400-e29b-41d4-a716-446655440000</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      DELETE /api/organization-invitations/550e8400-e29b-41d4-a716-446655440000
    /// </remarks>
    /// <response code="204">Invitation canceled successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to cancel the invitation or is not the inviter.</response>
    /// <response code="404">Invitation not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpDelete("{id:guid}", Name = "CancelInvitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        var requestingUserId = GetCurrentUserId();
        await invitationService.CancelInvitationAsync(id, requestingUserId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Resends an invitation by generating a new token and extending the expiration date.
    /// </summary>
    /// <param name="id">The unique identifier of the invitation to resend.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Success status of the resend operation.</returns>
    /// <example>POST /api/organization-invitations/550e8400-e29b-41d4-a716-446655440000/resend</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-invitations/550e8400-e29b-41d4-a716-446655440000/resend
    /// </remarks>
    /// <response code="200">Invitation resent successfully.</response>
    /// <response code="400">Invalid invitation or invitation cannot be resent.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to resend the invitation or is not the inviter.</response>
    /// <response code="404">Invitation not found.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("{id:guid}/resend", Name = "ResendInvitation")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Resend(Guid id, CancellationToken cancellationToken = default)
    {
        var requestingUserId = GetCurrentUserId();
        var result = await invitationService.ResendInvitationAsync(id, requestingUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cleans up expired invitations from the system (Admin operation).
    /// </summary>
    /// <returns>Success status of the cleanup operation.</returns>
    /// <example>POST /api/organization-invitations/cleanup-expired</example>
    /// <remarks>
    /// Sample Request:
    ///
    ///      POST /api/organization-invitations/cleanup-expired
    /// 
    /// This is typically an administrative operation that should be run periodically
    /// to remove expired invitations from the system.
    /// </remarks>
    /// <response code="200">Cleanup completed successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have permission to perform cleanup operations.</response>
    /// <response code="500">An internal server error occurred.</response>
    [HttpPost("cleanup-expired", Name = "CleanupExpiredInvitations")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CleanupExpired()
    {
        var result = await invitationService.CleanupExpiredInvitationsAsync();
        return Ok(result);
    }
}