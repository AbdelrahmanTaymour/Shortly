using FluentValidation.TestHelper;
using Shortly.Core.Admin.ResetUserUsage;

namespace Shortly.Core.UnitTests.Admin;

public class ResetUserUsageCommandValidatorTests
{
    private readonly ResetUserUsageCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAnId()
    {
        // Act
        var result = _sut.TestValidate(new ResetUserUsageCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId()
    {
        // Act
        var result = _sut.TestValidate(new ResetUserUsageCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
