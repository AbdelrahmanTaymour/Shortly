namespace Shortly.Core.DTOs.UsersDTOs.Usage;

public record UpdateUserUsageDto(
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    int TotalLinksCreated,
    int TotalQrCodesCreated,
    DateTime MonthlyResetDate);