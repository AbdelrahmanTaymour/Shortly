using FluentValidation.TestHelper;
using Shortly.Core.Account.VerifyEmail;

namespace Shortly.Core.UnitTests.Account;

public class VerifyEmailCommandValidatorTests
{
    private readonly VerifyEmailCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForANonBlankToken()
    {
        // Act
        var result = _sut.TestValidate(new VerifyEmailCommand("token"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_ForABlankToken(string token)
    {
        // Act
        var result = _sut.TestValidate(new VerifyEmailCommand(token));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
