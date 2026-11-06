using FluentValidation.TestHelper;
using Shortly.Core.Organizations.DeleteOrganization;

namespace Shortly.Core.UnitTests.Organizations;

public class DeleteOrganizationCommandValidatorTests
{
    private readonly DeleteOrganizationCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new DeleteOrganizationCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId_InOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(new DeleteOrganizationCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }
}
