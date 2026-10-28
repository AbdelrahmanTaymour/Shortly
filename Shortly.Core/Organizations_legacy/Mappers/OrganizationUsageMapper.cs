using Shortly.Core.Organizations_legacy.DTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Organizations_legacy.Mappers;

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