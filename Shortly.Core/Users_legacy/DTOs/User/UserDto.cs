using Shortly.Domain.Enums;
using Shortly.Domain.Users;

namespace Shortly.Core.Users_legacy.DTOs.User;

public sealed record UserDto(
    Guid Id,
    string Email,
    string Username,
    UserRole Role,
    enSubscriptionPlan SubscriptionPlanId,
    bool IsActive,
    bool IsEmailConfirmed,
    DateTime UpdatedAt,
    DateTime CreatedAt,
    bool IsDeleted,
    DateTime? DeletedAt,
    Guid? DeletedBy
);