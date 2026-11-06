using FluentValidation.TestHelper;
using Shortly.Core.Teams.UpdateTeam;

namespace Shortly.Core.UnitTests.Teams;

public class UpdateTeamCommandValidatorTests
{
    private readonly UpdateTeamCommandValidator _sut = new();

    private static UpdateTeamCommand Valid() => new(Guid.NewGuid(), Guid.NewGuid(), "Marketing", "Campaign links");

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_WhenTheNameAndDescriptionAreOmitted()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Name = null, Description = null });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankOrganizationOrTeamId()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { OrganizationId = Guid.Empty, TeamId = Guid.Empty });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
        result.ShouldHaveValidationErrorFor(x => x.TeamId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_ForABlankName_WhenOneIsSent(string name)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Name = name });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validate_Should_EnforceTheNameLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Name = new string('n', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Name);
        else result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Validate_Should_EnforceTheDescriptionLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Description = new string('d', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Description);
        else result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
