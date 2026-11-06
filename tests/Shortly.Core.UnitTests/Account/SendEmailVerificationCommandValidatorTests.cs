using FluentValidation.TestHelper;
using Shortly.Core.Account.SendEmailVerification;

namespace Shortly.Core.UnitTests.Account;

public class SendEmailVerificationCommandValidatorTests
{
    private readonly SendEmailVerificationCommandValidator _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("jane@example.com")]
    public void Validate_Should_Pass_WhenTheEmailIsOmittedOrWellFormed(string? email)
    {
        // Act
        var result = _sut.TestValidate(new SendEmailVerificationCommand(email));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForAMalformedEmail()
    {
        // Act
        var result = _sut.TestValidate(new SendEmailVerificationCommand("not-an-email"));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
