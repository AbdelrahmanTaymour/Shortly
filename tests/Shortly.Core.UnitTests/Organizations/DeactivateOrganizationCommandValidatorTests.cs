using FluentValidation.TestHelper;
using Shortly.Core.Organizations.DeactivateOrganization;

namespace Shortly.Core.UnitTests.Organizations;

public class DeactivateOrganizationCommandValidatorTests
{
    private readonly DeactivateOrganizationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new DeactivateOrganizationCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new DeactivateOrganizationCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }
}
