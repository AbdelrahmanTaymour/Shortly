using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs;

public record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    enUserRole Role,
    bool IsActive,
    bool IsEmailConfirmed,
    DateTime? LastLoginAt,
    string? TimeZone,
    string? ProfilePictureUrl,
    int MonthlyLinksCreated,
    int TotalLinksCreated,
    DateTime MonthlyResetDate,
    bool TwoFactorEnabled,
    DateTime CreatedAt,
    DateTime UpdatedAt
);