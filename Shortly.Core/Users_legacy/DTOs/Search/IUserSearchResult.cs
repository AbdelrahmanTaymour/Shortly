using Shortly.Domain.Enums;

namespace Shortly.Core.Users_legacy.DTOs.Search;

public abstract record IUserSearchResult(
    Guid Id,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    bool IsActive
);