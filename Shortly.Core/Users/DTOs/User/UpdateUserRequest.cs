using Shortly.Domain.Enums;

namespace Shortly.Core.Users.DTOs.User;

public record UpdateUserRequest(
    string Username,
    enSubscriptionPlan SubscriptionPlanId,
    long Permissions,
    bool IsActive,
    bool IsEmailConfirmed
);