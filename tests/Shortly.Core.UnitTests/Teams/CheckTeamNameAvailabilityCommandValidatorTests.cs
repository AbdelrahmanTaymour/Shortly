using FluentValidation.TestHelper;
using Shortly.Core.Teams.CheckTeamNameAvailability;

namespace Shortly.Core.UnitTests.Teams;

public class CheckTeamNameAvailabilityCommandValidatorTests
{
    private readonly CheckTeamNameAvailabilityCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand_WithAndWithoutAnExcludedTeam()
    {
        // Act
        var plain = _sut.TestValidate(new CheckTeamNameAvailabilityCommand(Guid.NewGuid(), "Sales"));
        var excluding = _sut.TestValidate(new CheckTeamNameAvailabilityCommand(Guid.NewGuid(), "Sales", Guid.NewGuid()));

        // Assert
        plain.ShouldNotHaveAnyValidationErrors();
        excluding.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new CheckTeamNameAvailabilityCommand(Guid.Empty, "Sales"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_ForABlankName(string name)
    {
        // Act
        var result = _sut.TestValidate(new CheckTeamNameAvailabilityCommand(Guid.NewGuid(), name));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_EnforceTheNameLengthBoundary()
    {
        // Act
        var atLimit = _sut.TestValidate(new CheckTeamNameAvailabilityCommand(Guid.NewGuid(), new string('a', 100)));
        var overLimit = _sut.TestValidate(new CheckTeamNameAvailabilityCommand(Guid.NewGuid(), new string('a', 101)));

        // Assert
        atLimit.ShouldNotHaveValidationErrorFor(x => x.Name);
        overLimit.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnEmptyExcludedTeamId()
    {
        // Act
        var result = _sut.TestValidate(new CheckTeamNameAvailabilityCommand(Guid.NewGuid(), "Sales", Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExcludeTeamId);
    }
}
