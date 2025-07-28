using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs;

public sealed record CreateUserResponse
(
    Guid Id,
    string Name,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    enUserRole Role,
    string? ProfilePictureUrl,
    string? TimeZone,
    bool IsActive,
    DateTime CreatedAt
);