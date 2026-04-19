using Shortly.Core.Profile.DTOs;
using Shortly.Core.Security.DTOs;
using Shortly.Core.Users.DTOs.Usage;
using Shortly.Domain.Enums;

namespace Shortly.Core.Users.DTOs.Search;

public sealed record CompleteUserSearchResult(
    Guid Id,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlanId,
    long Permissions,
    bool IsActive,
    bool IsEmailConfirmed,
    DateTime UpdatedAt,
    DateTime CreatedAt,
    bool IsDeleted,
    DateTime? DeletedAt,
    Guid? DeletedBy,

    // Related entities dto
    UserProfileResponse UserProfile,
    UserSecurityDto UserSecurityDto,
    UserUsageDto UserUsageDto
) : IUserSearchResult(Id, Email, Username, SubscriptionPlanId, IsActive, Permissions);