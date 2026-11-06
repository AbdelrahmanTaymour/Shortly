using FluentValidation.TestHelper;
using Shortly.Core.Account.Register;

namespace Shortly.Core.UnitTests.Account;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    private static RegisterCommand Valid() => new("jane@example.com", "jane", "Str0ng-password!");

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    public void Validate_Should_Fail_ForABlankOrMalformedEmail(string email)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Email = email });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_Should_Fail_ForABlankOrTooShortUsername(string username)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Username = username });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Validate_Should_EnforceTheUsernameLengthBoundaries(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Username = new string('a', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Username);
        else result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Validate_Should_Fail_ForABlankOrTooShortPassword(string password)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Password = password });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_Should_AcceptAnEightCharacterPassword()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Password = "12345678" });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
