using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs;

public record CreateUserRequest
(
    string Name,
    string Email,
    string Username,
    string Password,
    enSubscriptionPlan SubscriptionPlan,
    enUserRole Role,
    string? ProfilePictureUrl,
    string? TimeZone,
    bool IsActive
);