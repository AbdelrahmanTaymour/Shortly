using Shortly.Domain.Members;

namespace Shortly.Core.UnitTests.TestData;

internal static class MemberData
{
    public static OrganizationMember Create(Guid organizationId, Guid userId, MemberRole role, bool isActive = true)
    {
        var member = role == MemberRole.OrgOwner
            ? OrganizationMember.CreateOwner(organizationId, userId, TestClock.UtcNow)
            : OrganizationMember.Invite(organizationId, userId, role, Guid.NewGuid(), TestClock.UtcNow).Value;

        if (!isActive) member.Deactivate();
        return member;
    }
}
