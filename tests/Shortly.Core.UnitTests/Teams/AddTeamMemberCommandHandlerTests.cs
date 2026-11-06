using Shortly.Core.Teams.AddTeamMember;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

public class AddTeamMemberCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IOrganizationTeamMemberRepository _teamMembers = Substitute.For<IOrganizationTeamMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OrganizationTeam _team;
    private readonly AddTeamMemberCommandHandler _sut;

    public AddTeamMemberCommandHandlerTests()
    {
        _team = TeamData.Create(_orgId, Guid.NewGuid());
        _teams.GetByIdAsync(_team.Id, Arg.Any<CancellationToken>()).Returns(_team);
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new AddTeamMemberCommandHandler(
            _authorization.Service, _members, _teams, _teamMembers, _unitOfWork, TestClock.Provider());
    }

    private OrganizationMember Target(bool isActive = true, Guid? organizationId = null)
    {
        var member = MemberData.Create(organizationId ?? _orgId, Guid.NewGuid(), MemberRole.Member, isActive);
        _members.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);
        return member;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTeamDoesNotExistOrBelongsToAnotherOrganization()
    {
        // Act
        var missing = await _sut.Handle(new AddTeamMemberCommand(_orgId, Guid.NewGuid(), Guid.NewGuid()), default);
        var otherOrg = await _sut.Handle(new AddTeamMemberCommand(Guid.NewGuid(), _team.Id, Guid.NewGuid()), default);

        // Assert
        missing.Error.Should().Be(OrganizationTeamErrors.NotFound);
        otherOrg.Error.Should().Be(OrganizationTeamErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsNotAMember()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, _team.Id, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_When_TheCallerNeitherManagesTheTeamNorIsAnAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);
        var target = Target();

        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, _team.Id, target.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        _teamMembers.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_AllowTheTeamsOwnManager()
    {
        // Arrange
        var manager = MemberData.Create(_orgId, _callerId, MemberRole.TeamManager);
        var team = TeamData.Create(_orgId, manager.Id, "Managed");
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _authorization.Service.RequireMembershipAsync(_orgId, Arg.Any<CancellationToken>()).Returns(Result.Success(manager));
        var target = Target();

        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, team.Id, target.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberNotOrgMember_When_TheTargetDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, _team.Id, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.MemberNotOrgMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberNotOrgMember_When_TheTargetWasRemoved()
    {
        // Arrange
        var target = Target(isActive: false);

        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, _team.Id, target.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.MemberNotOrgMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberNotOrgMember_When_TheTargetBelongsToAnotherOrganization()
    {
        // Arrange
        var target = Target(organizationId: Guid.NewGuid());

        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, _team.Id, target.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.MemberNotOrgMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyTeamMember_When_TheTargetIsAlreadyOnTheTeam()
    {
        // Arrange
        var target = Target();
        _teamMembers.IsMemberOfTeamAsync(target.Id, _team.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, _team.Id, target.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.AlreadyTeamMember);
        _teamMembers.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_AddTheMemberToTheTeam_AndSave()
    {
        // Arrange
        var target = Target();

        // Act
        var result = await _sut.Handle(new AddTeamMemberCommand(_orgId, _team.Id, target.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _teamMembers.Received(1).Add(Arg.Is<OrganizationTeamMember>(m =>
            m.TeamId == _team.Id && m.MemberId == target.Id && m.JoinedAt == TestClock.UtcNow));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
