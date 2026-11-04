namespace Shortly.Infrastructure.EdgeProtection.Models;

/// <summary>
/// Sign-in attempts, per client. The per-account lockout cannot stop one client trying many different accounts
/// (credential stuffing); this does. Generous enough for several people behind one address.
/// </summary>
public sealed class LoginRateLimitOptions
{
    public const string SectionName = "RateLimiting:Login";
    public const string PolicyName = "Login";

    public int PermitLimit { get; init; } = 20;
    public int WindowSeconds { get; init; } = 300;
}