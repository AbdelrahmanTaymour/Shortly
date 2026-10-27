namespace Shortly.Core.Organizations_legacy.DTOs;

public sealed record OrganizationUsageDto(
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    int TotalLinksCreated,
    int TotalQrCodesCreated,
    DateTime MonthlyResetDate
);