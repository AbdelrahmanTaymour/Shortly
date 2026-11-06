using Shortly.Core.Teams.ChangeTeamManager;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

public class ChangeTeamManagerCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationMemberRepository _members = Substitute.For<IOrganizationMemberRepository>();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IOrganizationTeamMemberRepository _teamMembers = Substitute.For<IOrganizationTeamMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _currentManagerId = Guid.NewGuid();
    private readonly OrganizationTeam _team;
    private readonly ChangeTeamManagerCommandHandler _sut;

    public ChangeTeamManagerCommandHandlerTests()
    {
        _team = TeamData.Create(_orgId, _currentManagerId);
        _teams.GetByIdAsync(_team.Id, Arg.Any<CancellationToken>()).Returns(_team);
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new ChangeTeamManagerCommandHandler(
            _authorization.Service, _members, _teams, _teamMembers, _unitOfWork, TestClock.Provider());
    }

    private OrganizationMember NewManager(bool isActive = true, Guid? organizationId = null, MemberRole role = MemberRole.TeamManager)
    {
        var member = MemberData.Create(organizationId ?? _orgId, Guid.NewGuid(), role, isActive);
        _members.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);
        return member;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTeamDoesNotExistOrBelongsToAnotherOrganization()
    {
        // Act
        var missing = await _sut.Handle(new ChangeTeamManagerCommand(_orgId, Guid.NewGuid(), Guid.NewGuid()), default);
        var otherOrg = await _sut.Handle(new ChangeTeamManagerCommand(Guid.NewGuid(), _team.Id, Guid.NewGuid()), default);

        // Assert
        missing.Error.Should().Be(OrganizationTeamErrors.NotFound);
        otherOrg.Error.Should().Be(OrganizationTeamErrors.NotFound);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_When_TheCallerIsBelowAdmin_EvenTheCurrentManager(MemberRole role)
    {
        // Arrange: only an admin may hand a team to someone else
        _authorization.CallerIs(_orgId, _callerId, role);
        var newManager = NewManager();

        // Act
        var result = await _sut.Handle(new ChangeTeamManagerCommand(_orgId, _team.Id, newManager.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        _team.TeamManagerId.Should().Be(_currentManagerId);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsNotAMember()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(new ChangeTeamManagerCommand(_orgId, _team.Id, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Handle_Should_ReturnManagerNotOrgMember_ForAMissingRemovedOrForeignMember(bool removed, bool foreign)
    {
        // Arrange
        var newManagerId = Guid.NewGuid();
        if (removed || foreign)
            newManagerId = NewManager(isActive: !removed, organizationId: foreign ? Guid.NewGuid() : null).Id;

        // Act
        var result = await _sut.Handle(new ChangeTeamManagerCommand(_orgId, _team.Id, newManagerId), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerNotOrgMember);
        _team.TeamManagerId.Should().Be(_currentManagerId);
    }

    [Fact]
    public async Task Handle_Should_ChangeTheManager_AddThemToTheTeam_AndSave()
    {
        // Arrange
        var newManager = NewManager();

        // Act
        var result = await _sut.Handle(new ChangeTeamManagerCommand(_orgId, _team.Id, newManager.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _team.TeamManagerId.Should().Be(newManager.Id);
        _teamMembers.Received(1).Add(Arg.Is<OrganizationTeamMember>(m => m.TeamId == _team.Id && m.MemberId == newManager.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotAddTheNewManagerAgain_When_TheyAreAlreadyOnTheTeam()
    {
        // Arrange
        var newManager = NewManager();
        _teamMembers.IsMemberOfTeamAsync(newManager.Id, _team.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _sut.Handle(new ChangeTeamManagerCommand(_orgId, _team.Id, newManager.Id), default);

        // Assert
        _teamMembers.DidNotReceiveWithAnyArgs().Add(default!);
        _team.TeamManagerId.Should().Be(newManager.Id);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    public async Task Handle_Should_ReturnManagerRoleTooLow_AndChangeNothing_When_TheNewManagerIsBelowTeamManager(MemberRole role)
    {
        // Arrange
        var newManager = NewManager(role: role);

        // Act
        var result = await _sut.Handle(new ChangeTeamManagerCommand(_orgId, _team.Id, newManager.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerRoleTooLow);
        _team.TeamManagerId.Should().NotBe(newManager.Id);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
