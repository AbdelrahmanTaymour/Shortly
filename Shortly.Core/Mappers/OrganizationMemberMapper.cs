using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class OrganizationMemberMapper
{
    public static OrganizationMemberDto MapToOrganizationMemberDto(this OrganizationMember organizationMember)
    {
        return new OrganizationMemberDto(
            organizationMember.Id,
            organizationMember.OrganizationId,
            organizationMember.UserId,
            organizationMember.RoleId,
            organizationMember.CustomPermissions,
            organizationMember.IsActive,
            organizationMember.InvitedBy,
            organizationMember.JoinedAt
        );
    }

    public static IEnumerable<OrganizationMemberDto> MapToOrganizationMemberDtos(this IEnumerable<OrganizationMember> organizationMembers)
    {
        return organizationMembers.Select(MapToOrganizationMemberDto);
    }
}