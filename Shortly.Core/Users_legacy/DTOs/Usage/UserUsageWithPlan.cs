using Shortly.Domain.Enums;

namespace Shortly.Core.Users_legacy.DTOs.Usage;

public record UserUsageWithPlan(
    enSubscriptionPlan SubscriptionPlanId,
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    DateTime MonthlyResetDate,
    int TotalLinksCreated,
    int TotalQrCodesCreated
);