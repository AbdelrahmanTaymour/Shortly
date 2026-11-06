using FluentValidation.TestHelper;
using Shortly.Core.Organizations.SwitchOrganization;

namespace Shortly.Core.UnitTests.Organizations;

public class SwitchOrganizationCommandValidatorTests
{
    private readonly SwitchOrganizationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new SwitchOrganizationCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new SwitchOrganizationCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }
}
