using Shortly.Core.Teams.CreateTeam;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

public class CreateTeamCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IOrganizationTeamMemberRepository _teamMembers = Substitute.For<IOrganizationTeamMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateTeamCommandHandler _sut;

    public CreateTeamCommandHandlerTests()
    {
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new CreateTeamCommandHandler(
            _authorization.Service, _members, _teams, _teamMembers, _unitOfWork, TestClock.Provider());
    }

    private OrganizationMember ActiveManager(MemberRole role = MemberRole.TeamManager, Guid? organizationId = null)
    {
        var manager = MemberData.Create(organizationId ?? _orgId, Guid.NewGuid(), role);
        _members.GetByIdAsync(manager.Id, Arg.Any<CancellationToken>()).Returns(manager);
        return manager;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsNotAMember()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, Guid.NewGuid(), "Team", null), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_AndCreateNothing_When_TheCallerIsBelowAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);
        var manager = ActiveManager();

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, manager.Id, "Team", null), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        _teams.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnManagerNotOrgMember_When_TheManagerDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, Guid.NewGuid(), "Team", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerNotOrgMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnManagerNotOrgMember_When_TheManagerWasRemoved()
    {
        // Arrange
        var inactive = MemberData.Create(_orgId, Guid.NewGuid(), MemberRole.Member, isActive: false);
        _members.GetByIdAsync(inactive.Id, Arg.Any<CancellationToken>()).Returns(inactive);

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, inactive.Id, "Team", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerNotOrgMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnManagerNotOrgMember_When_TheManagerBelongsToAnotherOrganization()
    {
        // Arrange
        var outsider = ActiveManager(organizationId: Guid.NewGuid());

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, outsider.Id, "Team", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerNotOrgMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnNameTaken_When_TheOrganizationAlreadyHasATeamWithThatName()
    {
        // Arrange
        var manager = ActiveManager();
        _teams.IsTeamNameExistAsync("Team", _orgId, Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, manager.Id, "Team", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NameTaken);
        _teams.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnNameRequired_When_TheNameIsBlank()
    {
        // Arrange
        var manager = ActiveManager();

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, manager.Id, "  ", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NameRequired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateTheTeam_AddTheManagerAsAMember_AndReturnTheTeamId()
    {
        // Arrange
        var manager = ActiveManager();

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, manager.Id, "Marketing", "Campaign links"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _teams.Received(1).Add(Arg.Is<OrganizationTeam>(t =>
            t.Id == result.Value && t.OrganizationId == _orgId && t.TeamManagerId == manager.Id
            && t.Name == "Marketing" && t.Description == "Campaign links"));
        _teamMembers.Received(1).Add(Arg.Is<OrganizationTeamMember>(m => m.TeamId == result.Value && m.MemberId == manager.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public async Task Handle_Should_AcceptAnActiveMemberWithAtLeastTheTeamManagerRoleAsManager(MemberRole role)
    {
        // Arrange
        var manager = ActiveManager(role);

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, manager.Id, "Team", null), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    public async Task Handle_Should_ReturnManagerRoleTooLow_AndCreateNothing_When_TheManagerIsBelowTeamManager(MemberRole role)
    {
        // Arrange
        var manager = ActiveManager(role);

        // Act
        var result = await _sut.Handle(new CreateTeamCommand(_orgId, manager.Id, "Team", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerRoleTooLow);
        _teams.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
