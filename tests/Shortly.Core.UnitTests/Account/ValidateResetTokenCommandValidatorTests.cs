using FluentValidation.TestHelper;
using Shortly.Core.Account.ValidateResetToken;

namespace Shortly.Core.UnitTests.Account;

public class ValidateResetTokenCommandValidatorTests
{
    private readonly ValidateResetTokenCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForANonBlankToken()
    {
        // Act
        var result = _sut.TestValidate(new ValidateResetTokenCommand("token"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_ForABlankToken(string token)
    {
        // Act
        var result = _sut.TestValidate(new ValidateResetTokenCommand(token));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
