namespace Shortly.Infrastructure.EdgeProtection.Models;

/// <summary>Attempts to submit a password to a password-protected short URL, per client.</summary>
public sealed class PasswordVerificationRateLimitOptions
{
    public const string SectionName = "RateLimiting:PasswordVerification";
    public const string PolicyName = "PasswordVerification";

    public int PermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 300;
}