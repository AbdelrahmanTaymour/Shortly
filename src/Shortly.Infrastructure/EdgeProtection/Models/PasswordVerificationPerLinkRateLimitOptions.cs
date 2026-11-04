namespace Shortly.Infrastructure.EdgeProtection.Models;

public sealed class PasswordVerificationPerLinkRateLimitOptions
{
    public const string SectionName = "RateLimiting:PasswordVerificationPerLink";

    public int PermitLimit { get; init; } = 600;
    public int WindowSeconds { get; init; } = 3600;
}