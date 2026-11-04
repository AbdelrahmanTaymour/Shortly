namespace Shortly.Infrastructure.EdgeProtection.Models;

/// <summary>Account registrations, per client.</summary>
public sealed class RegistrationRateLimitOptions
{
    public const string SectionName = "RateLimiting:Registration";
    public const string PolicyName = "Registration";

    public int PermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 3600;
}