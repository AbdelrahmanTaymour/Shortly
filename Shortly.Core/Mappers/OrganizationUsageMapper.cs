using Shortly.Core.DTOs.OrganizationDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class OrganizationUsageMapper
{
    public static OrganizationUsageDto MapToOrganizationUsageDto(this OrganizationUsage usage)
    {
        return new OrganizationUsageDto(
            usage.MonthlyLinksCreated,
            usage.MonthlyQrCodesCreated,
            usage.TotalLinksCreated,
            usage.TotalQrCodesCreated,
            usage.MonthlyResetDate
        );
    }

    public static IEnumerable<OrganizationUsageDto> MapToOrganizationUsageDtos(
        this IEnumerable<OrganizationUsage> usages)
    {
        return usages.Select(MapToOrganizationUsageDto);
    }
}