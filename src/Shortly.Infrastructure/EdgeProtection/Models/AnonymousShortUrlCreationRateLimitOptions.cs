namespace Shortly.Infrastructure.EdgeProtection.Models;

public sealed class AnonymousShortUrlCreationRateLimitOptions
{
    public const string SectionName = "RateLimiting:AnonymousShortUrlCreation";
    public const string PolicyName = "AnonymousShortUrlCreation";

    public int PermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 60;
}