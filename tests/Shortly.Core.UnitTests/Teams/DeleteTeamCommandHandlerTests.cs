using Shortly.Core.Teams.DeleteTeam;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

public class DeleteTeamCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DeleteTeamCommandHandler _sut;

    public DeleteTeamCommandHandlerTests()
    {
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new DeleteTeamCommandHandler(_authorization.Service, _teams, _unitOfWork);
    }

    private OrganizationTeam TeamInTheRepository(Guid? organizationId = null)
    {
        var team = TeamData.Create(organizationId ?? _orgId, Guid.NewGuid());
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        return team;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTeamDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new DeleteTeamCommand(_orgId, Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTeamBelongsToAnotherOrganization()
    {
        // Arrange
        var team = TeamInTheRepository(organizationId: Guid.NewGuid());

        // Act
        var result = await _sut.Handle(new DeleteTeamCommand(_orgId, team.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NotFound);
        _teams.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_When_TheCallerIsNotAMember()
    {
        // Arrange
        var team = TeamInTheRepository();
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(new DeleteTeamCommand(_orgId, team.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_AndDeleteNothing_When_TheCallerIsBelowAdmin(MemberRole role)
    {
        // Arrange
        var team = TeamInTheRepository();
        _authorization.CallerIs(_orgId, _callerId, role);

        // Act
        var result = await _sut.Handle(new DeleteTeamCommand(_orgId, team.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        _teams.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_RemoveTheTeam_AndSave_When_TheCallerIsAnAdmin()
    {
        // Arrange
        var team = TeamInTheRepository();

        // Act
        var result = await _sut.Handle(new DeleteTeamCommand(_orgId, team.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _teams.Received(1).Remove(team);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotLetTheTeamsOwnManagerDeleteIt_BecauseDeletingNeedsAnAdmin()
    {
        // Arrange
        var team = TeamInTheRepository();
        _authorization.CallerIs(_orgId, _callerId, MemberRole.TeamManager);
        _authorization.Caller!.Id.Should().NotBe(team.TeamManagerId);

        // Act
        var result = await _sut.Handle(new DeleteTeamCommand(_orgId, team.Id), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
    }
}
