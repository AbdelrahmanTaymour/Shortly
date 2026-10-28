using Shortly.Domain.Enums;

namespace Shortly.Core.Users_legacy.DTOs.User;

public record CreateUserResponse(
    Guid Id,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlanId,
    bool IsActive,
    DateTime CreatedAt
    );