using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Core.UnitTests.TestData;

internal static class OrganizationData
{
    public static Organization Create(
        Guid? ownerId = null,
        string name = "Acme",
        int memberLimit = 10,
        enSubscriptionPlan plan = enSubscriptionPlan.Free) =>
        Organization.Create(name, null, null, null, memberLimit, ownerId ?? Guid.NewGuid(), plan, TestClock.UtcNow).Value;

    public static Organization CreateInactive(Guid? ownerId = null)
    {
        var organization = Create(ownerId);
        organization.Deactivate(TestClock.UtcNow);
        return organization;
    }
}
