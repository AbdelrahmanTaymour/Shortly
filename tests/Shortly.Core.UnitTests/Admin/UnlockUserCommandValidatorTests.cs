using FluentValidation.TestHelper;
using Shortly.Core.Admin.UnlockUser;

namespace Shortly.Core.UnitTests.Admin;

public class UnlockUserCommandValidatorTests
{
    private readonly UnlockUserCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAnId()
    {
        // Act
        var result = _sut.TestValidate(new UnlockUserCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId()
    {
        // Act
        var result = _sut.TestValidate(new UnlockUserCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
