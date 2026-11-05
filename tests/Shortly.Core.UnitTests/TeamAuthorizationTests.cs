using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Teams;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests;

/// <summary>Team changes need the team's manager or an organization admin/owner; membership is read from the database.</summary>
public class TeamAuthorizationTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();

    private IOrganizationAuthorizationService Authorization => new OrganizationAuthorizationService(TestData.Caller(_callerId), _members);

    private OrganizationTeam TeamManagedBy(Guid managerMemberId) =>
        OrganizationTeam.Create(_orgId, managerMemberId, "Team", null, TestData.UtcNow).Value;

    private OrganizationMember CallerIs(MemberRole role, bool active = true)
    {
        var member = TestData.Member(_orgId, _callerId, role, active);
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>())
            .Returns(active ? member : null);
        return member;
    }

    [Fact]
    public async Task TheTeamsOwnManager_HasAuthority_EvenWithoutAnAdminRole()
    {
        var manager = CallerIs(MemberRole.TeamManager);

        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(manager.Id), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task AnAdminOrOwner_HasAuthorityOverAnyTeam(MemberRole role)
    {
        CallerIs(role);

        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]   // a manager of a DIFFERENT team
    public async Task OtherMembers_AreDeniedWithInsufficientRole(MemberRole role)
    {
        CallerIs(role);

        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(Guid.NewGuid()), default);

        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }

    [Fact]
    public async Task ARemovedManager_LosesAuthority()
    {
        var manager = CallerIs(MemberRole.TeamManager, active: false);

        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(manager.Id), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task ANonMember_IsDeniedAsNotAMember()
    {
        _members.GetActiveByOrganizationAndUserAsync(_orgId, _callerId, Arg.Any<CancellationToken>()).Returns((OrganizationMember?)null);

        var result = await TeamAuthorization.RequireAuthorityAsync(Authorization, TeamManagedBy(Guid.NewGuid()), default);

        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }
}
