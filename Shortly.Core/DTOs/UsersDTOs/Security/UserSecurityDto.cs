namespace Shortly.Core.DTOs.UsersDTOs.Security;

public sealed record UserSecurityDto(
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    bool TwoFactorEnabled,
    string? TwoFactorSecret,
    DateTime UpdatedAt
);