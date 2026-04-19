using Shortly.Domain.Enums;

namespace Shortly.Core.Users.DTOs.Usage;

public record UserUsageWithPlan(
    enSubscriptionPlan SubscriptionPlanId,
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    DateTime MonthlyResetDate,
    int TotalLinksCreated,
    int TotalQrCodesCreated
);