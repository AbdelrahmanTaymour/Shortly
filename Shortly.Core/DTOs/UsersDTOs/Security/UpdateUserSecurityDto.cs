namespace Shortly.Core.DTOs.UsersDTOs.Security;

public record UpdateUserSecurityDto(
    int FailedLoginAttempts,
    DateTime? LockedUntil,
    bool TwoFactorEnabled,
    string? TwoFactorSecret,
    string? PasswordResetToken,
    DateTime? TokenExpiresAt);