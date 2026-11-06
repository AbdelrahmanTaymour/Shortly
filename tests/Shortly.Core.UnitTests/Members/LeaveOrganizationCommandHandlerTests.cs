using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Members.LeaveOrganization;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Members;

public class LeaveOrganizationCommandHandlerTests
{
    private static readonly AccessTokenResponse PersonalTokens = new("personal-access", "personal-refresh", 900);

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IOrganizationTeamMemberRepository _teamMembers = Substitute.For<IOrganizationTeamMemberRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LeaveOrganizationCommandHandler _sut;

    public LeaveOrganizationCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _jwtService.IssueTokensAsync(_user, Arg.Any<CancellationToken>()).Returns(PersonalTokens);
        _sut = new LeaveOrganizationCommandHandler(
            CallerData.Create(_user.Id, organizationId: _orgId), _users, _members, _teams, _teamMembers, _jwtService, _unitOfWork);
    }

    private OrganizationMember Membership(MemberRole role, bool isActive = true)
    {
        var member = MemberData.Create(_orgId, _user.Id, role, isActive);
        _members.GetByOrganizationAndUserAsync(_orgId, _user.Id, Arg.Any<CancellationToken>()).Returns(member);
        return member;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAuthenticated_When_TheCallerNoLongerExists()
    {
        // Arrange
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(new LeaveOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(AuthenticationErrors.NotAuthenticated);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerHasNoMembership()
    {
        // Act
        var result = await _sut.Handle(new LeaveOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheMembershipIsAlreadyInactive()
    {
        // Arrange
        Membership(MemberRole.Member, isActive: false);

        // Act
        var result = await _sut.Handle(new LeaveOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnOwnerCannotLeave_AndKeepTheOwnerActive()
    {
        // Arrange
        var owner = Membership(MemberRole.OrgOwner);

        // Act
        var result = await _sut.Handle(new LeaveOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.OwnerCannotLeave);
        owner.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberManagesTeams_AndKeepTheMembership_When_TheCallerManagesATeam()
    {
        // Arrange
        var member = Membership(MemberRole.TeamManager);
        _teams.IsManagerOfAnyTeamAsync(member.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(new LeaveOrganizationCommand(_orgId), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.MemberManagesTeams);
        member.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    public async Task Handle_Should_DeactivateTheMembership_RemoveTeamMemberships_SaveAndReturnPersonalTokens(MemberRole role)
    {
        // Arrange
        var member = Membership(role);
        var teamMembership = OrganizationTeamMember.Create(Guid.NewGuid(), member.Id, TestClock.UtcNow);
        _teamMembers.GetByMemberAsync(member.Id, Arg.Any<CancellationToken>()).Returns([teamMembership]);

        // Act
        var result = await _sut.Handle(new LeaveOrganizationCommand(_orgId), default);

        // Assert
        result.Value.Should().Be(PersonalTokens);
        member.IsActive.Should().BeFalse();
        _teamMembers.Received(1).Remove(teamMembership);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _jwtService.IssueTokensAsync(_user, Arg.Any<CancellationToken>());
        });
    }
}
