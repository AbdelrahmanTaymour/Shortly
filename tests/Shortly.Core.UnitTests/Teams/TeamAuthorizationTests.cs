using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Teams;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

/// <summary>Team changes need the team's manager or an organization admin/owner; membership is read from the database.</summary>
public class TeamAuthorizationTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private IOrganizationAuthorizationService Authorization =>
        new OrganizationAuthorizationService(CallerData.Create(_callerId), _members);

    private OrganizationTeam TeamManagedBy(Guid managerMemberId) => TeamData.Create(_orgId, managerMemberId, "Team");

    private OrganizationMember CallerIs(MemberRole role, bool active = true)
    {
        var member = MemberData.Create(_orgId, _callerId, role, active);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>())
            .Returns(active ? member : null);
        return member;
    }

    [Fact]
    public async Task RequireAuthorityAsync_Should_Succeed_ForTheTeamsOwnManager_EvenWithoutAnAdminRole()
    {
        // Arrange
        var manager = CallerIs(MemberRole.TeamManager);

        // Act
        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(manager.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task RequireAuthorityAsync_Should_Succeed_ForAnAdminOrOwner_OfAnyTeam(MemberRole role)
    {
        // Arrange
        CallerIs(role);

        // Act
        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(Guid.NewGuid()), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)] // a manager of a DIFFERENT team
    public async Task RequireAuthorityAsync_Should_ReturnInsufficientRole_ForOtherMembers(MemberRole role)
    {
        // Arrange
        CallerIs(role);

        // Act
        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    [Fact]
    public async Task RequireAuthorityAsync_Should_ReturnNotAMember_ForAManagerWhoWasRemoved()
    {
        // Arrange
        var manager = CallerIs(MemberRole.TeamManager, active: false);

        // Act
        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(manager.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task RequireAuthorityAsync_Should_ReturnNotAMember_ForANonMember()
    {
        // Arrange
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>()).Returns((OrganizationMember?)null);

        // Act
        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task RequireAuthorityAsync_Should_RefuseAViewerWhoIsStillAssignedAsManager()
    {
        // Arrange: the assigned manager keeps authority only while they hold the Team Manager role
        var viewer = CallerIs(MemberRole.Viewer);

        // Act
        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(viewer.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }
}
