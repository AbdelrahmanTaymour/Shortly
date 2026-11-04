namespace Shortly.Infrastructure.EdgeProtection.Models;

/// <summary>Endpoints that email an address the caller chooses (verification, email change, password reset), per client.</summary>
public sealed class AccountEmailRateLimitOptions
{
    public const string SectionName = "RateLimiting:AccountEmail";
    public const string PolicyName = "AccountEmail";

    public int PermitLimit { get; init; } = 5;
    public int WindowSeconds { get; init; } = 3600;
}