using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs.User;

public record UpdateUserDto(
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlanId,
    long Permissions,
    bool IsActive,
    bool IsEmailConfirmed,
    bool IsDeleted,
    Guid DeletedBy
);