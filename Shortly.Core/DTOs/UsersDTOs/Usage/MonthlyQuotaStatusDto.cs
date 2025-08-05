using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs.Usage;

public record MonthlyQuotaStatusDto(
    int RemainingLinksThisMonth,
    int RemainingQrCodesThisMonth,
    int DaysUntilQuotaReset,

// Additional useful properties
    int MaxLinksPerMonth,
    int MaxQrCodesPerMonth,
    int UsedLinksThisMonth,
    int UsedQrCodesThisMonth,
    enSubscriptionPlan SubscriptionPlan,
    DateTime QuotaResetDate,

// Calculated properties for convenience
    double LinksUsagePercentage,
    double QrCodesUsagePercentage,
    bool IsLinksQuotaExhausted,
    bool IsQrCodesQuotaExhausted,
    bool IsNearLinksQuotaLimit,
    bool IsNearQrCodesQuotaLimit
);