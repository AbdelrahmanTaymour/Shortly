using Shortly.Domain.Enums;

namespace Shortly.Core.Users.DTOs.Search;

public sealed record UserSearchRequest(
    string? SearchTerm,
    enSubscriptionPlan? SubscriptionPlan,
    bool? IsActive,
    bool? IsDeleted,
    bool? IsEmailConfirmed,
    int Page = 1,
    int PageSize = 10
);