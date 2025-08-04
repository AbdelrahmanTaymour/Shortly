using Shortly.Core.DTOs.UsersDTOs.Profile;
using Shortly.Core.DTOs.UsersDTOs.Security;
using Shortly.Core.DTOs.UsersDTOs.Usage;
using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs.Search;

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
    UserProfileDto UserProfile,
    UserSecurityDto UserSecurityDto,
    UserUsageDto UserUsageDto
) : IUserSearchResult(Id, Email, Username, SubscriptionPlanId, IsActive, Permissions);