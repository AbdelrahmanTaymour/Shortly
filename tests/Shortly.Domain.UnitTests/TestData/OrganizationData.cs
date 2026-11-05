using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Domain.UnitTests.TestData;

internal static class OrganizationData
{
    public static Organization CreateOrganization(
        string name = "Acme",
        Guid? ownerId = null,
        int memberLimit = 10,
        enSubscriptionPlan plan = enSubscriptionPlan.Free,
        DateTime? utcNow = null) =>
        Organization.Create(name, null, null, null, memberLimit, ownerId ?? Guid.NewGuid(), plan, utcNow ?? TestClock.Now).Value;

    public static OrganizationMember CreateMember(
        MemberRole role = MemberRole.Member,
        Guid? organizationId = null,
        Guid? userId = null,
        Guid? invitedBy = null) =>
        OrganizationMember.Invite(organizationId ?? Guid.NewGuid(), userId ?? Guid.NewGuid(), role,
            invitedBy ?? Guid.NewGuid(), TestClock.Now).Value;

    public static OrganizationMember CreateOwner(Guid? organizationId = null, Guid? userId = null) =>
        OrganizationMember.CreateOwner(organizationId ?? Guid.NewGuid(), userId ?? Guid.NewGuid(), TestClock.Now);
}
