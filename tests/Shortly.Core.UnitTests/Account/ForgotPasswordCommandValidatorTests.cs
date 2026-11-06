using FluentValidation.TestHelper;
using Shortly.Core.Account.ForgotPassword;

namespace Shortly.Core.UnitTests.Account;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidEmail()
    {
        // Act
        var result = _sut.TestValidate(new ForgotPasswordCommand("jane@example.com"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Validate_Should_Fail_ForABlankOrMalformedEmail(string email)
    {
        // Act
        var result = _sut.TestValidate(new ForgotPasswordCommand(email));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
