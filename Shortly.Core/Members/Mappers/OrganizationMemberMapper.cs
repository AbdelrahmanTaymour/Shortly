using Shortly.Core.Members.DTOs;
using Shortly.Domain.Entities;
using Shortly.Domain.Members;

namespace Shortly.Core.Members.Mappers;

public static class OrganizationMemberMapper
{
    public static OrganizationMemberDto MapToOrganizationMemberDto(this OrganizationMember organizationMember)
    {
        return new OrganizationMemberDto(
            organizationMember.Id,
            organizationMember.OrganizationId,
            organizationMember.UserId,
            organizationMember.Role,
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