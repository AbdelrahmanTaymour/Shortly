using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs;

public record UserDto
(
    Guid Id,
    string Name,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    enUserRole Role,
    bool IsActive,
    bool IsDeleted,
    bool IsEmailConfirmed,
    DateTime? LastLoginAt,
    string? TimeZone,
    string? ProfilePictureUrl,
    DateTime UpdatedAt,
    DateTime CreatedAt,
    DateTime? DeletedAt,
    Guid? DeletedBy,
    int MonthlyLinksCreated,
    int TotalLinksCreated,
    DateTime MonthlyResetDate,
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    bool TwoFactorEnabled,
    string? TwoFactorSecret
);