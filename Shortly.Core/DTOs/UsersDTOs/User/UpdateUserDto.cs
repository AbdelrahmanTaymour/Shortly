using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs.User;

public record UpdateUserDto(
    string Username,
    enSubscriptionPlan SubscriptionPlanId,
    long Permissions,
    bool IsActive,
    bool IsEmailConfirmed
);