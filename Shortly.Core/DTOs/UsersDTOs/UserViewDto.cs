using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs;

public sealed record UserViewDto
(
    Guid Id,
    string Name,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    enUserRole Role,
    bool IsActive
);