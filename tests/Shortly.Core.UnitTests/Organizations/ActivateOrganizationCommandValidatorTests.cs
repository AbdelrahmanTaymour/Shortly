using FluentValidation.TestHelper;
using Shortly.Core.Organizations.ActivateOrganization;

namespace Shortly.Core.UnitTests.Organizations;

public class ActivateOrganizationCommandValidatorTests
{
    private readonly ActivateOrganizationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new ActivateOrganizationCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new ActivateOrganizationCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }
}
