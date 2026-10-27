using Shortly.Domain.Enums;

namespace Shortly.Core.Users_legacy.DTOs.Search;

public sealed record UserSearchResult(
    Guid Id,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    bool IsActive
) : IUserSearchResult(Id, Email, Username, SubscriptionPlan, IsActive);