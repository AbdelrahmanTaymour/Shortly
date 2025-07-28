using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UsersDTOs;

public sealed record UpdateUserDto(
    string Name,
    string Email,
    string Username,
    enSubscriptionPlan SubscriptionPlan,
    enUserRole Role,
    bool IsActive,
    bool IsEmailConfirmed,
    string? TimeZone,
    string? ProfilePictureUrl,
    int MonthlyLinksCreated,
    int TotalLinksCreated,
    DateTime MonthlyResetDate,
    int FailedLoginAttempts,
    DateTime? LockedUntil
);