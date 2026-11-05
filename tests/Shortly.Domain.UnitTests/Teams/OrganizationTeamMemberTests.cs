using Shortly.Domain.Teams;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Teams;

public class OrganizationTeamMemberTests
{
    [Fact]
    public void Create_Should_LinkTheMemberToTheTeam_AtTheGivenTime()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        // Act
        var teamMember = OrganizationTeamMember.Create(teamId, memberId, TestClock.Now);

        // Assert
        teamMember.Id.Should().NotBeEmpty();
        teamMember.TeamId.Should().Be(teamId);
        teamMember.MemberId.Should().Be(memberId);
        teamMember.JoinedAt.Should().Be(TestClock.Now);
    }

    [Fact]
    public void Create_Should_GenerateADifferentIdEachTime()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        // Act
        var first = OrganizationTeamMember.Create(teamId, memberId, TestClock.Now);
        var second = OrganizationTeamMember.Create(teamId, memberId, TestClock.Now);

        // Assert
        first.Id.Should().NotBe(second.Id);
    }
}
