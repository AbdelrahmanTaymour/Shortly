using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class OrganizationTeamMapper
{
    public static OrganizationTeamDto MapToOrganizationTeamDto(this OrganizationTeam team)
    {
        return new OrganizationTeamDto(
            team.Id,
            team.OrganizationId,
            team.TeamManagerId,
            team.Name,
            team.Description,
            team.CreatedAt
        );
    }

    public static IEnumerable<OrganizationTeamDto> MapToOrganizationTeamDtos(
        this IEnumerable<OrganizationTeam> members)
    {
        return members.Select(MapToOrganizationTeamDto);
    }
}