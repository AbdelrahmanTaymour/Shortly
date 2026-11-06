using FluentValidation.TestHelper;
using Shortly.Core.Teams.ChangeTeamManager;

namespace Shortly.Core.UnitTests.Teams;

public class ChangeTeamManagerCommandValidatorTests
{
    private readonly ChangeTeamManagerCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new ChangeTeamManagerCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new ChangeTeamManagerCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InTeamId()
    {
        // Act
        var result = _sut.TestValidate(new ChangeTeamManagerCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeamId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InNewManagerId()
    {
        // Act
        var result = _sut.TestValidate(new ChangeTeamManagerCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewManagerId);
    }
}
