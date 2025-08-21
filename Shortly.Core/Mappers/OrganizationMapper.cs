using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class OrganizationMapper
{
    public static OrganizationDto MapToOrganizationDto(this Organization org)
    {
        return new OrganizationDto(
            org.Id,
            org.OwnerId,
            org.Name,
            org.Description,
            org.Website,
            org.LogoUrl,
            org.MemberLimit,
            org.IsActive,
            org.IsSubscribed,
            org.CreatedAt,
            org. UpdatedAt,
            org.DeletedAt
        );
    }

    public static IEnumerable<OrganizationDto> MapToOrganizationDtos(this IEnumerable<Organization> orgs)
    {
        return orgs.Select(MapToOrganizationDto);
    }
}