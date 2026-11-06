using Shortly.Core.Teams.CheckTeamNameAvailability;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;

namespace Shortly.Core.UnitTests.Teams;

public class CheckTeamNameAvailabilityCommandHandlerTests
{
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly AuthorizationData _authorization = new();
    private readonly IOrganizationTeamRepository _teams = Substitute.For<IOrganizationTeamRepository>();
    private readonly CheckTeamNameAvailabilityCommandHandler _sut;

    public CheckTeamNameAvailabilityCommandHandlerTests()
    {
        _authorization.CallerIs(_orgId, Guid.NewGuid(), MemberRole.Viewer);
        _sut = new CheckTeamNameAvailabilityCommandHandler(_authorization.Service, _teams);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotAMember_AndNotQuery_When_TheCallerIsNotAMember()
    {
        // Arrange
        _authorization.CallerIsNotAMember(_orgId);

        // Act
        var result = await _sut.Handle(new CheckTeamNameAvailabilityCommand(_orgId, "Sales"), default);

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotAMember);
        await _teams.DidNotReceiveWithAnyArgs().IsTeamNameExistAsync(default!, default, default, default);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Handle_Should_ReturnWhetherTheNameIsFree(bool taken, bool expected)
    {
        // Arrange
        _teams.IsTeamNameExistAsync("Sales", _orgId, null, Arg.Any<CancellationToken>()).Returns(taken);

        // Act
        var result = await _sut.Handle(new CheckTeamNameAvailabilityCommand(_orgId, "Sales"), default);

        // Assert
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_Should_ExcludeTheTeamBeingRenamed_FromTheCheck()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        await _sut.Handle(new CheckTeamNameAvailabilityCommand(_orgId, "Sales", teamId), default);

        // Assert
        await _teams.Received(1).IsTeamNameExistAsync("Sales", _orgId, teamId, Arg.Any<CancellationToken>());
    }
}
