using FluentValidation.TestHelper;
using Shortly.Core.Account.Login;

namespace Shortly.Core.UnitTests.Account;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new LoginCommand("jane@example.com", "any-password"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_Should_Fail_ForABlankOrMalformedEmail(string email)
    {
        // Act
        var result = _sut.TestValidate(new LoginCommand(email, "any-password"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankPassword()
    {
        // Act
        var result = _sut.TestValidate(new LoginCommand("jane@example.com", ""));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_Should_NotApplyThePasswordLengthRule_SoOlderShortPasswordsCanStillSignIn()
    {
        // Act
        var result = _sut.TestValidate(new LoginCommand("jane@example.com", "abc"));

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
