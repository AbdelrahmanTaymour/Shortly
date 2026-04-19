namespace Shortly.Core.Users.DTOs.Usage;

public sealed record UserUsageDto(
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    int TotalLinksCreated,
    int TotalQrCodesCreated,
    DateTime MonthlyResetDate
);