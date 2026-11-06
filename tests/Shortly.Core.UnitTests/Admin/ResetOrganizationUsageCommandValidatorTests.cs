using FluentValidation.TestHelper;
using Shortly.Core.Admin.ResetOrganizationUsage;

namespace Shortly.Core.UnitTests.Admin;

public class ResetOrganizationUsageCommandValidatorTests
{
    private readonly ResetOrganizationUsageCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAnId()
    {
        // Act
        var result = _sut.TestValidate(new ResetOrganizationUsageCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId()
    {
        // Act
        var result = _sut.TestValidate(new ResetOrganizationUsageCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }
}
