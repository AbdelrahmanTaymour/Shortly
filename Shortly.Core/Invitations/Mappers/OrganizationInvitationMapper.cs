using Shortly.Core.Invitations.DTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Invitations.Mappers;

public static class OrganizationInvitationMapper
{
    public static OrganizationInvitationDto MapToOrganizationInvitationDto(
        this OrganizationInvitation invitation)
    {
        return new OrganizationInvitationDto(
            invitation.Id,
            invitation.OrganizationId,
            invitation.InvitedUserEmail,
            invitation.InvitedUserRoleId,
            invitation.InvitedUserPermissions,
            invitation.InvitedBy,
            invitation.Status,
            invitation.RegisteredAt,
            invitation.ExpiresAt,
            invitation.CreatedAt
        );
    }

    public static IEnumerable<OrganizationInvitationDto> MapToOrganizationInvitationDtos(
        this IEnumerable<OrganizationInvitation> invitations)
    {
        return invitations.Select(MapToOrganizationInvitationDto);
    }
}