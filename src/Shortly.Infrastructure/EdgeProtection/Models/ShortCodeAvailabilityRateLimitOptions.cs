namespace Shortly.Infrastructure.EdgeProtection.Models;

/// <summary>"Is this short code free?" lookups, per client (the endpoint enumerates which codes exist).</summary>
public sealed class ShortCodeAvailabilityRateLimitOptions
{
    public const string SectionName = "RateLimiting:ShortCodeAvailability";
    public const string PolicyName = "ShortCodeAvailability";

    public int PermitLimit { get; init; } = 60;
    public int WindowSeconds { get; init; } = 60;
}