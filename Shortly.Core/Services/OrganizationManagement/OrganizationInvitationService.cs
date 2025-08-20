using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.RepositoryContract.OrganizationManagement;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.OrganizationManagement;

public class OrganizationInvitationService(
    IOrganizationInvitationRepository invitationRepository,
    IOrganizationRepository organizationRepository,
    IOrganizationMemberRepository memberRepository,
    ILogger<OrganizationInvitationService> logger) : IOrganizationInvitationService
{
    public async Task<OrganizationInvitation> CreateInvitationAsync(Guid organizationId, InviteMemberDto dto, CancellationToken cancellationToken = default)
    {
        // Check if an organization exists and has capacity
        var organization = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if(organization == null)
            throw new NotFoundException("Organization", organizationId);
        
        // Check if the org reached the member limits
        var currentMemberCount =
            await memberRepository.GetMemberCountByOrganizationAsync(organizationId, cancellationToken);
        
        if(currentMemberCount >= organization.MemberLimit)
            throw new BusinessRuleException("Organization has reached member limit.");
        
        // Check if the user is already invited or a member
        var isInvited = await invitationRepository.HasPendingInvitationAsync(dto.Email, organizationId, cancellationToken);
        if (isInvited)
            throw new InvalidOperationException("User already has a pending invitation");

        // Create invitation
        var invitation = new OrganizationInvitation
        {
            OrganizationId = organizationId,
            InvitedUserEmail = dto.Email,
            InvitedBy = dto.InvitedBy,
            InvitationToken = GenerateInvitationToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        
        var created = await invitationRepository.AddAsync(invitation, cancellationToken);
        logger.LogInformation("Invitation created for {Email} to organization {OrganizationId}", dto.Email, organizationId);
        return created;
    }

    public async Task<OrganizationInvitation?> GetInvitationByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
        if(invitation == null)
            throw new NotFoundException("Invitation", token);
        
        return invitation;
    }

    public async Task<IEnumerable<OrganizationInvitation>> GetOrganizationInvitationsAsync(Guid organizationId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await invitationRepository.GetByOrganizationIdAsync(organizationId, page, pageSize, cancellationToken);
    }

    public async Task<bool> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
        if (invitation == null || invitation.Status == enInvitationStatus.Failure || invitation.Status == enInvitationStatus.Registered || invitation.Status == enInvitationStatus.Rejected || invitation.IsExpired)
            throw new ValidationException("Invalid invitation.");

        // Add user as member
        await memberRepository.AddAsync(new OrganizationMember
        {
            OrganizationId = invitation.OrganizationId,
            UserId = userId,
            RoleId = enUserRole.Member, // Default role
            InvitedBy = invitation.InvitedBy,
            JoinedAt = DateTime.UtcNow
        }, cancellationToken);

        // Update invitation status
        await invitationRepository.AcceptInvitationAsync(invitation.Id, cancellationToken);

        logger.LogInformation("User {UserId} accepted invitation to organization {OrganizationId}", userId, invitation.OrganizationId);
        return true;
    }

    public async Task<bool> RejectInvitationAsync(string token, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
        if (invitation == null || invitation.IsExpired ||
            invitation.Status == enInvitationStatus.Failure || 
            invitation.Status == enInvitationStatus.Registered || 
            invitation.Status == enInvitationStatus.Rejected)
            throw new ValidationException("Invalid invitation.");
        
        // Update invitation status
        return await invitationRepository.RejectInvitationAsync(invitation.Id, cancellationToken);
    }

    public async Task<bool> CancelInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if(invitation == null)
            throw new NotFoundException("Invitation", invitationId);
        
        if(invitation.InvitedBy != requestingUserId)
            throw new ForbiddenException("Only the inviter can cancel the invitation.");
        
        return await invitationRepository.DeleteAsync(invitationId, cancellationToken);
    }

    public async Task<bool> ResendInvitationAsync(Guid invitationId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByIdAsync(invitationId, cancellationToken);
        if (invitation == null || 
            invitation.InvitedBy != requestingUserId ||
            invitation.Status != enInvitationStatus.Pending ||
            invitation.Status != enInvitationStatus.EmailSent)
            throw new ValidationException("Invalid invitation.");

        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        invitation.InvitationToken = GenerateInvitationToken();
        return await invitationRepository.UpdateAsync(invitation, cancellationToken);
    }

    public async Task<bool> CleanupExpiredInvitationsAsync()
    {
        return await invitationRepository.CleanupExpiredInvitationsAsync();
    }

    public async Task<bool> ValidateInvitationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
        return invitation != null &&
               invitation.Status == enInvitationStatus.Pending &&
               invitation.Status == enInvitationStatus.EmailSent &&
               invitation.Status == enInvitationStatus.UserClicked &&
               !invitation.IsExpired;
    }
    
    
    private string GenerateInvitationToken()
    {
        return Guid.NewGuid().ToString("N")[..16] + DateTime.UtcNow.Ticks.ToString("X");
    }
}