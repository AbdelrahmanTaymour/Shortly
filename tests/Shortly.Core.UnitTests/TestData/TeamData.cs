using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.TestData;

internal static class TeamData
{
    public static OrganizationTeam Create(Guid organizationId, Guid managerMemberId, string name = "Marketing") =>
        OrganizationTeam.Create(organizationId, managerMemberId, name, "Campaign links", TestClock.UtcNow).Value;
}
