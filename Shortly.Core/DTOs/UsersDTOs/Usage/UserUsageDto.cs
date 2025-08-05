namespace Shortly.Core.DTOs.UsersDTOs.Usage;

public sealed record UserUsageDto(
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    int TotalLinksCreated,
    int TotalQrCodesCreated,
    DateTime MonthlyResetDate
);