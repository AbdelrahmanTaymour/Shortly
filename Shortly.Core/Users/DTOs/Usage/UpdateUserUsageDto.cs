namespace Shortly.Core.Users.DTOs.Usage;

public record UpdateUserUsageDto(
    int? MonthlyLinksCreated,
    int? MonthlyQrCodesCreated,
    int? TotalLinksCreated,
    int? TotalQrCodesCreated,
    DateTime? MonthlyResetDate);