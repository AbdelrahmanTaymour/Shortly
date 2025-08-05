using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs.Usage;

public record UserUsageWithPlan(
    enSubscriptionPlan SubscriptionPlanId,
    int MonthlyLinksCreated,
    int MonthlyQrCodesCreated,
    DateTime MonthlyResetDate,
    int TotalLinksCreated,
    int TotalQrCodesCreated
);