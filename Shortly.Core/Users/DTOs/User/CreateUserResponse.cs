using Shortly.Domain.Enums;

namespace Shortly.Core.Users.DTOs.User;

public record CreateUserResponse(
    Guid Id,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlanId,
    long Permissions,
    bool IsActive,
    DateTime CreatedAt
    );