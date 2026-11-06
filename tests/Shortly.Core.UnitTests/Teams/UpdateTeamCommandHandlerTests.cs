using Shortly.Core.Teams.UpdateTeam;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

public class UpdateTeamCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OrganizationTeam _team;
    private readonly UpdateTeamCommandHandler _sut;

    public UpdateTeamCommandHandlerTests()
    {
        _team = TeamData.Create(_orgId, Guid.NewGuid(), "Marketing");
        _teams.GetByIdAsync(_team.Id, Arg.Any<CancellationToken>()).Returns(_team);
        _authorization.CallerIs(_orgId, _callerId, MemberRole.OrgAdmin);
        _sut = new UpdateTeamCommandHandler(_authorization.Service, _teams, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTeamDoesNotExistOrBelongsToAnotherOrganization()
    {
        // Act
        var missing = await _sut.Handle(new UpdateTeamCommand(_orgId, Guid.NewGuid(), "New", null), default);
        var otherOrg = await _sut.Handle(new UpdateTeamCommand(Guid.NewGuid(), _team.Id, "New", null), default);

        // Assert
        missing.Error.Should().Be(OrganizationTeamErrors.NotFound);
        otherOrg.Error.Should().Be(OrganizationTeamErrors.NotFound);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task Handle_Should_ReturnInsufficientRole_AndChangeNothing_When_TheCallerNeitherManagesTheTeamNorIsAnAdmin(MemberRole role)
    {
        // Arrange
        _authorization.CallerIs(_orgId, _callerId, role);

        // Act
        var result = await _sut.Handle(new UpdateTeamCommand(_orgId, _team.Id, "Renamed", "Changed"), default);

        // Assert
        result.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        _team.Name.Should().Be("Marketing");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RenameTheTeamAndChangeTheDescription_AndSave()
    {
        // Act
        var result = await _sut.Handle(new UpdateTeamCommand(_orgId, _team.Id, "Growth", "New description"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _team.Name.Should().Be("Growth");
        _team.Description.Should().Be("New description");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_LeaveTheOtherPropertyAlone_WhenOnlyOneIsSent()
    {
        // Act
        await _sut.Handle(new UpdateTeamCommand(_orgId, _team.Id, null, "Only the description"), default);

        // Assert
        _team.Name.Should().Be("Marketing");
        _team.Description.Should().Be("Only the description");
    }

    [Fact]
    public async Task Handle_Should_ReturnNameRequired_AndSaveNothing_When_TheNewNameIsBlank()
    {
        // Act
        var result = await _sut.Handle(new UpdateTeamCommand(_orgId, _team.Id, "   ", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NameRequired);
        _team.Name.Should().Be("Marketing");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnNameTaken_AndChangeNothing_When_AnotherTeamAlreadyHasTheNewName()
    {
        // Arrange
        _teams.IsTeamNameExistAsync("Sales", _orgId, _team.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(new UpdateTeamCommand(_orgId, _team.Id, "Sales", null), default);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NameTaken);
        _team.Name.Should().Be("Marketing");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AllowACaseOnlyRename_WithoutCheckingUniqueness()
    {
        // Act
        var result = await _sut.Handle(new UpdateTeamCommand(_orgId, _team.Id, "MARKETING", null), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _team.Name.Should().Be("MARKETING");
        await _teams.DidNotReceiveWithAnyArgs().IsTeamNameExistAsync(default!, default, default, default);
    }

    [Fact]
    public async Task Handle_Should_NotCheckUniqueness_When_TheNameIsOmitted()
    {
        // Act
        var result = await _sut.Handle(new UpdateTeamCommand(_orgId, _team.Id, null, "Only this"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _teams.DidNotReceiveWithAnyArgs().IsTeamNameExistAsync(default!, default, default, default);
    }
}
