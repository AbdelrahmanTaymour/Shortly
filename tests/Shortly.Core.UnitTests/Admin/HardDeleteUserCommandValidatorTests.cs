using FluentValidation.TestHelper;
using Shortly.Core.Admin.HardDeleteUser;

namespace Shortly.Core.UnitTests.Admin;

public class HardDeleteUserCommandValidatorTests
{
    private readonly HardDeleteUserCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAnId()
    {
        // Act
        var result = _sut.TestValidate(new HardDeleteUserCommand(Guid.NewGuid()));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankId()
    {
        // Act
        var result = _sut.TestValidate(new HardDeleteUserCommand(Guid.Empty));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
