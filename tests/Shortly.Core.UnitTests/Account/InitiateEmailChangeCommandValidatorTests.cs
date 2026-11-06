using FluentValidation.TestHelper;
using Shortly.Core.Account.InitiateEmailChange;

namespace Shortly.Core.UnitTests.Account;

public class InitiateEmailChangeCommandValidatorTests
{
    private readonly InitiateEmailChangeCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(new InitiateEmailChangeCommand("new@example.com", "current-password"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_Should_Fail_ForABlankOrMalformedNewEmail(string newEmail)
    {
        // Act
        var result = _sut.TestValidate(new InitiateEmailChangeCommand(newEmail, "current-password"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewEmail);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankCurrentPassword()
    {
        // Act
        var result = _sut.TestValidate(new InitiateEmailChangeCommand("new@example.com", ""));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }
}
