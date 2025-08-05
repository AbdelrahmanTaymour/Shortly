using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs.Search;

public sealed record UserSearchResult(
    Guid Id,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    bool IsActive,
    long Permissions
) : IUserSearchResult(Id, Email, Username, SubscriptionPlan, IsActive, Permissions);