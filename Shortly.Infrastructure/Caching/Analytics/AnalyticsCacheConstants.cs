using Microsoft.Extensions.Caching.Memory;

namespace Shortly.Infrastructure.Caching.Analytics;

/// <summary>
/// Contains both the Query and Command cache decorators cleanly separated
/// to strictly adhere to the Interface Segregation Principle (ISP).
/// </summary>
public static class AnalyticsCacheConstants
{
    public const string PfxCount   = "repo:analytics:total-count";
    public const string PfxClicks  = "repo:analytics:clicks";
    public const string PfxPopular = "repo:analytics:popular";
    public const string PfxUser    = "repo:analytics:user-summary";
    public const string PfxOrg     = "repo:analytics:org-summary";

    public static MemoryCacheEntryOptions Opts(TimeSpan ttl) =>
        new() { AbsoluteExpirationRelativeToNow = ttl, Size = 1 };
}