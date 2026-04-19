namespace Shortly.Core.Security.DTOs;

public sealed record UserSecurityDto(
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    bool TwoFactorEnabled,
    string? TwoFactorSecret,
    DateTime UpdatedAt
);