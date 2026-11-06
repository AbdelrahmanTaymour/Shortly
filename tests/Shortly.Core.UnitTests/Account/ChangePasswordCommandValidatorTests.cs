using FluentValidation.TestHelper;
using Shortly.Core.Account.ChangePassword;

namespace Shortly.Core.UnitTests.Account;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new ChangePasswordCommand("current", "N3w-password!"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankCurrentPassword()
    {
        // Act
        var result = _sut.TestValidate(new ChangePasswordCommand("", "N3w-password!"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Validate_Should_Fail_ForABlankOrTooShortNewPassword(string newPassword)
    {
        // Act
        var result = _sut.TestValidate(new ChangePasswordCommand("current", newPassword));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
