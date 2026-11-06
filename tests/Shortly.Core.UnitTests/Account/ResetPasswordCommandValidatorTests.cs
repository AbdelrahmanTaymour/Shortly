using FluentValidation.TestHelper;
using Shortly.Core.Account.ResetPassword;

namespace Shortly.Core.UnitTests.Account;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new ResetPasswordCommand("token", "N3w-password!"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankToken()
    {
        // Act
        var result = _sut.TestValidate(new ResetPasswordCommand("", "N3w-password!"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Validate_Should_Fail_ForABlankOrTooShortNewPassword(string newPassword)
    {
        // Act
        var result = _sut.TestValidate(new ResetPasswordCommand("token", newPassword));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
