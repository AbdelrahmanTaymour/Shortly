using FluentValidation.TestHelper;
using Shortly.Core.Teams.DeleteTeam;

namespace Shortly.Core.UnitTests.Teams;

public class DeleteTeamCommandValidatorTests
{
    private readonly DeleteTeamCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new DeleteTeamCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new DeleteTeamCommand(Guid.Empty, Guid.NewGuid()));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InTeamId()
    {
        // Act
        var result = _sut.TestValidate(new DeleteTeamCommand(Guid.NewGuid(), Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeamId);
    }
}
