using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Extensions;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.ServiceContracts.Email;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.OrganizationManagement;

/// <summary>
/// Provides operations for managing organization invitations,
/// including creation, retrieval, acceptance, rejection, cancellation,
/// resending, cleanup, and validation.
/// </summary>
public class OrganizationInvitationService(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    IUserService userService,
    IEmailNotificationService notificationService,
    IConfiguration configuration,
    ILogger<OrganizationInvitationService> logger) : IOrganizationInvitationService
{
    /// <inheritdoc />
    public async Task<OrganizationInvitationDto> CreateInvitationAsync(Guid organizationId, InviteMemberDto dto, CancellationToken cancellationToken = default)
    {
        var organization = await ValidateInvitationCreationRequest(organizationId, dto, cancellationToken);
        var expiryDays = Convert.ToInt16(configuration["AppSettings:Tokens:InvitationExpiryDays"] ?? "7");
        
        // Create invitation
        var invitation = new OrganizationInvitation
        {
            OrganizationId = organizationId,
            InvitedUserEmail = dto.Email,
            InvitedUserRoleId = dto.RoleId,
            InvitedUserPermissions = dto.CustomPermissions,
            InvitedBy = dto.InvitedBy,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow
        };
        
        var invitationCreated = await invitationRepository.AddAsync(invitation, cancellationToken);
        logger.LogInformation("Invitation created for {Email} to organization {OrganizationId}", dto.Email, organizationId);
        
        // Send Email invitation
        var invitationToken = Sha256Extensions.Encrypt(invitationCreated.Id.ToString());
        var emailSent = await SendInvitationEmail(organization, invitationCreated, invitationToken, cancellationToken);
        if(emailSent) invitationCreated.Status = enInvitationStatus.EmailSent;
        
        return invitationCreated.MapToOrganizationInvitationDto();
    }

    /// <inheritdoc />
     public async Task<OrganizationInvitationDto> GetInvitationByIdAsync(Guid id, CancellationToken cancellationToken = default)
     {
         var invitation = await invitationRepository.GetByIdAsync(id, cancellationToken);
         if(invitation == null)
             throw new NotFoundException("Invitation", id);
         
         return invitation.MapToOrganizationInvitationDto();
     }

    /// <inheritdoc />
    public async Task<IEnumerable<OrganizationInvitationDto>> GetOrganizationInvitationsAsync(Guid organizationId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var invitations = await invitationRepository.GetByOrganizationIdAsync(organizationId, page, pageSize, cancellationToken);
        return invitations.MapToOrganizationInvitationDtos();
    }

    /// <inheritdoc />
    public async Task<bool> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
    {
        var id = Guid.Parse(Sha256Extensions.Decrypt(token) ?? throw new ValidationException("Invalid token invitation."));
        
        var invitation = await invitationRepository.GetByIdAsync(id, cancellationToken);
        if (invitation == null || invitation.Status == enInvitationStatus.Failure || invitation.Status == enInvitationStatus.Registered || invitation.Status == enInvitationStatus.Rejected || invitation.IsExpired)
            throw new ValidationException("Invalid invitation.");
        
        var user = await userService.GetByEmailAsync(invitation.InvitedUserEmail, cancellationToken);
        if(user.Id != userId)
            throw new ForbiddenException("The invited user does not match the current user.");
        
       
        // Add user as member
        await memberRepository.AddAsync(new OrganizationMember
        {
            OrganizationId = invitation.OrganizationId,
            UserId = userId,
            RoleId = invitation.InvitedUserRoleId,
            CustomPermissions = invitation.InvitedUserPermissions,
            InvitedBy = invitation.InvitedBy,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);

        // Update invitation status as Registered
        await invitationRepository.AcceptInvitationAsync(invitation.Id, cancellationToken);

        logger.LogInformation("User {UserId} accepted invitation to organization {OrganizationId}", userId, invitation.OrganizationId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RejectInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation == null || invitation.IsExpired ||
                invitation.Status == enInvitationStatus.Failure || 
                invitation.Status == enInvitationStatus.Registered || 
                invitation.Status == enInvitationStatus.Rejected)
            throw new ValidationException("Invalid invitation.");
        
        // Update invitation status
        return await invitationRepository.RejectInvitationAsync(invitation.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CancelInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if(invitation == null)
            throw new NotFoundException("Invitation", invitationId);
        
        if(invitation.InvitedBy != requestingUserId)
            throw new ForbiddenException("Only the inviter can cancel the invitation.");
        
        return await invitationRepository.DeleteAsync(invitationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ResendInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation == null || invitation.Status != enInvitationStatus.EmailSent)
            throw new ValidationException("Invalid invitation.");

        var organization = await organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
        if(organization == null)
            throw new NotFoundException("Organization", invitation.OrganizationId);
        
        // Resent invitation email
        var invitationToken = Sha256Extensions.Encrypt(invitation.Id.ToString()); 
        return await SendInvitationEmail(organization, invitation, invitationToken, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> CleanupExpiredInvitationsAsync()
    {
        return await invitationRepository.CleanupExpiredInvitationsAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ValidateInvitationTokenAsync(Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        return invitation != null && !invitation.IsExpired;
    }

    /// <summary>
    /// Validates whether an invitation can be created for a given organization and user.
    /// </summary>
    /// <param name="organizationId">The unique identifier of the target organization.</param>
    /// <param name="dto">The invitation request containing the invitee's details.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests while the operation is in progress.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the 
    /// <see cref="Organization"/> entity if the validation passes.
    /// </returns>
    /// <exception cref="NotFoundException">
    /// Thrown if the specified organization cannot be found.
    /// </exception>
    /// <exception cref="BusinessRuleException">
    /// Thrown if the organization has reached its maximum member capacity.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the invitee already has a pending invitation to the organization.
    /// </exception>
    /// <remarks>
    /// This method ensures that:
    /// <list type="bullet">
    /// <item>The organization exists.</item>
    /// <item>The organization has not exceeded its member limit.</item>
    /// <item>The invitee is not already invited or an existing member.</item>
    /// </list>
    /// </remarks>
    private async Task<Organization> ValidateInvitationCreationRequest(Guid organizationId, InviteMemberDto dto,
        CancellationToken cancellationToken)
    {
        // Check if an organization exists and has capacity
        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if(organization == null)
            throw new NotFoundException("Organization", organizationId);
        
        // Check if the org reached the member limits
        var currentMemberCount = await memberRepository.GetMemberCountByOrganizationAsync(organizationId, cancellationToken);
        if(currentMemberCount >= organization.MemberLimit)
            throw new BusinessRuleException("Organization has reached member limit.");
        
        // Check if the user is already invited or a member
        var isInvited = await invitationRepository.HasPendingInvitationAsync(dto.Email, organizationId, cancellationToken);
        if (isInvited)
            throw new InvalidOperationException("User already has a pending invitation");

        return organization;
    }

    /// <summary>
    /// Sends an invitation email to a user on behalf of an organization and updates the invitation status if successful.
    /// </summary>
    /// <param name="organization">The organization that is sending the invitation.</param>
    /// <param name="invitation">The invitation entity containing invitee details.</param>
    /// <param name="invitationToken">The optional token used for accepting the invitation.</param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests while the operation is in progress.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is <c>true</c> if the invitation 
    /// email was sent successfully; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// If the email is sent successfully, the invitation record is updated to reflect that it has been sent.
    /// The inviter's username is resolved from the organization membership list.
    /// </remarks>
    private async Task<bool> SendInvitationEmail(Organization organization, OrganizationInvitation invitation, string? invitationToken, CancellationToken cancellationToken = default)
    {
        var inviterUsername =
            await memberRepository.GetMemberUsernameAsync(invitation.OrganizationId, invitation.InvitedBy,
                cancellationToken) ?? "Unknown";
        var inviteeName = invitation.InvitedUserEmail.Split('@')[0];

        var result = await notificationService.SendUserInvitationAsync(invitation.InvitedUserEmail, inviterUsername,
            inviteeName, invitationToken,organization.Name);
        
        if(result.IsSuccess)
            await invitationRepository.MarkInvitationAsSentAsync(invitation.Id, cancellationToken);
        
        return result.IsSuccess;
    }
}