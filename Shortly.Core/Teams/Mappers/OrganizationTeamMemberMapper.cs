using Shortly.Core.Teams.DTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Teams.Mappers;

public static class OrganizationTeamMemberMapper
{
    public static OrganizationTeamMemberDto MapToOrganizationTeamMemberDto(
        this OrganizationTeamMember teamMember)
    {
        return new OrganizationTeamMemberDto(
            teamMember.Id,
            teamMember.TeamId,
            teamMember.MemberId,
            teamMember.JoinedAt
        );
    }

    public static IEnumerable<OrganizationTeamMemberDto> MapToOrganizationTeamMemberDtos(
        this IEnumerable<OrganizationTeamMember> teamMembers)
    {
        return teamMembers.Select(MapToOrganizationTeamMemberDto);
    }
}