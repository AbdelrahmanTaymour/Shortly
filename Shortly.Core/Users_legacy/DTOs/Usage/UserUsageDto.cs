namespace Shortly.Core.Users_legacy.DTOs.Usage;

public sealed record UserUsageDto(
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    int TotalLinksCreated,
    int TotalQrCodesCreated,
    DateTime MonthlyResetDate
);