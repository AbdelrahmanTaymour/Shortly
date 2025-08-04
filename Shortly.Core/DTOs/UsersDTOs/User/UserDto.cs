using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs.User;

public sealed record UserDto(
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
    Guid? DeletedBy
);