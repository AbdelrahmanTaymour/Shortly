namespace Shortly.Core.Security.DTOs;

public record UpdateUserSecurityDto(
    int? FailedLoginAttempts,
    DateTime? LockedUntil,
    bool TwoFactorEnabled,
    string? TwoFactorSecret,
    string? PasswordResetToken,
    DateTime? TokenExpiresAt);