using Shortly.Domain.Enums;

namespace Shortly.Core.Users.DTOs.Search;

public abstract record IUserSearchResult(
    Guid Id,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    bool IsActive,
    long Permissions
);