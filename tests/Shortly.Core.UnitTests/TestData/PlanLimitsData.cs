using Shortly.Core.Abstractions.Cache;

namespace Shortly.Core.UnitTests.TestData;

internal static class PlanLimitsData
{
    public static SubscriptionPlanLimits With(
        int maxLinks = 100,
        bool bulk = false,
        bool protection = false,
        bool customCode = false,
        int maxMembers = 10,
        int maxOrganizations = 1) =>
        new(maxLinks, 0, 0, maxMembers, maxOrganizations, false, bulk, protection, customCode, false, false);
}
