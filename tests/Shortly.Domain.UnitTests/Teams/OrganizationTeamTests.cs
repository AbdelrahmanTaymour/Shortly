using Shortly.Domain.Teams;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Teams;

public class OrganizationTeamTests
{
    private static readonly DateTime Now = TestClock.Now;

    private static OrganizationTeam NewTeam(Guid? managerId = null) =>
        OrganizationTeam.Create(Guid.NewGuid(), managerId ?? Guid.NewGuid(), "Marketing", "Campaign links", Now).Value;

    [Fact]
    public void Create_Should_SetTheInitialState()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        // Act
        var result = OrganizationTeam.Create(organizationId, managerId, "Marketing", "Campaign links", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var team = result.Value;
        team.Id.Should().NotBeEmpty();
        team.OrganizationId.Should().Be(organizationId);
        team.TeamManagerId.Should().Be(managerId);
        team.Name.Should().Be("Marketing");
        team.Description.Should().Be("Campaign links");
        team.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_Should_AllowAMissingDescription()
    {
        // Act
        var team = OrganizationTeam.Create(Guid.NewGuid(), Guid.NewGuid(), "Marketing", null, Now).Value;

        // Assert
        team.Description.Should().BeNull();
    }

    [Fact]
    public void Rename_Should_ReplaceTheName()
    {
        // Arrange
        var team = NewTeam();

        // Act
        var result = team.Rename("Growth");

        // Assert
        result.IsSuccess.Should().BeTrue();
        team.Name.Should().Be("Growth");
    }

    [Fact]
    public void UpdateDescription_Should_ReplaceAndClearTheDescription()
    {
        // Arrange
        var team = NewTeam();

        // Act
        team.UpdateDescription("New description");
        var afterSet = team.Description;
        team.UpdateDescription(null);

        // Assert
        afterSet.Should().Be("New description");
        team.Description.Should().BeNull();
    }

    [Fact]
    public void ChangeManager_Should_ReplaceTheManager()
    {
        // Arrange
        var team = NewTeam();
        var newManager = Guid.NewGuid();

        // Act
        var result = team.ChangeManager(newManager);

        // Assert
        result.IsSuccess.Should().BeTrue();
        team.TeamManagerId.Should().Be(newManager);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_TheNameIsBlank(string? name)
    {
        // Act
        var result = OrganizationTeam.Create(Guid.NewGuid(), Guid.NewGuid(), name!, null, Now);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NameRequired);
    }

    [Fact]
    public void Create_Should_Fail_When_TheManagerIsAnEmptyGuid()
    {
        // Act
        var result = OrganizationTeam.Create(Guid.NewGuid(), Guid.Empty, "Marketing", null, Now);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_Should_Fail_AndKeepTheName_When_TheNameIsBlank(string? name)
    {
        // Arrange
        var team = NewTeam();

        // Act
        var result = team.Rename(name!);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.NameRequired);
        team.Name.Should().Be("Marketing");
    }

    [Fact]
    public void ChangeManager_Should_Fail_AndKeepTheManager_When_TheNewManagerIsAnEmptyGuid()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var team = NewTeam(managerId);

        // Act
        var result = team.ChangeManager(Guid.Empty);

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.ManagerRequired);
        team.TeamManagerId.Should().Be(managerId);
    }
}
