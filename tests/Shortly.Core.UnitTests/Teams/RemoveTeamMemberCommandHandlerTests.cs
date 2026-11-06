using Shortly.Core.Teams.RemoveTeamMember;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

public class RemoveTeamMemberCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IOrganizationTeamMemberRepository _teamMembers = Substitute.For<IOrganizationTeamMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OrganizationTeam _team;
    private readonly RemoveTeamMemberCommandHandler _sut;

    public RemoveTeamMemberCommandHandlerTests()
    {
        _team = TeamData.Create(_orgId, _managerId);
        _teams.GetByIdAsync(_team.Id, Arg.Any<CancellationToken>()).Returns(_team);
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new RemoveTeamMemberCommandHandler(_authorization.Service, _teams, _teamMembers, _unitOfWork);
    }

    private OrganizationTeamMember TeamMembership(Guid memberId)
    {
        var membership = OrganizationTeamMember.Create(_team.Id, memberId, TestClock.UtcNow);
        _teamMembers.GetByTeamAndMemberAsync(_team.Id, memberId, Arg.Any<CancellationToken>()).Returns(membership);
        return membership;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTeamDoesNotExistOrBelongsToAnotherOrganization()
    {
        // Act
        var missing = await _sut.Handle(new RemoveTeamMemberCommand(_orgId, Guid.NewGuid(), Guid.NewGuid()), default);
        var otherOrg = await _sut.Handle(new RemoveTeamMemberCommand(Guid.NewGuid(), _team.Id, Guid.NewGuid()), default);

        // Assert
        missing.Error.Should().Be(OrganizationTeamErrors.NotFound);
        otherOrg.Error.Should().Be(OrganizationTeamErrors.NotFound);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_When_TheCallerNeitherManagesTheTeamNorIsAnAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);
        var memberId = Guid.NewGuid();
        TeamMembership(memberId);

        // Act
        var result = await _sut.Handle(new RemoveTeamMemberCommand(_orgId, _team.Id, memberId), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        _teamMembers.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotRemoveManager_WhenTheTargetIsTheTeamsManager()
    {
        // Arrange
        TeamMembership(_managerId);

        // Act
        var result = await _sut.Handle(new RemoveTeamMemberCommand(_orgId, _team.Id, _managerId), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.CannotRemoveManager);
        _teamMembers.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnMemberNotFound_When_TheTargetIsNotOnTheTeam()
    {
        // Act
        var result = await _sut.Handle(new RemoveTeamMemberCommand(_orgId, _team.Id, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.MemberNotFound);
    }

    [Fact]
    public async Task Handle_Should_RemoveTheTeamMembership_AndSave()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var membership = TeamMembership(memberId);

        // Act
        var result = await _sut.Handle(new RemoveTeamMemberCommand(_orgId, _team.Id, memberId), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _teamMembers.Received(1).Remove(membership);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
