using FluentValidation.TestHelper;
using Shortly.Core.Admin.CreateUser;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _sut = new();

    private static CreateUserCommand Valid() => new("jane@example.com", "jane", "secret1", UserRole.User);

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
    [InlineData("not-an-email")]
    public void Validate_Should_Fail_ForABlankOrMalformedEmail(string email)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Email = email });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_EnforceTheEmailLengthBoundary()
    {
        // Act
        var atLimit = _sut.TestValidate(Valid() with { Email = new string('a', 243) + "@example.com" });
        var overLimit = _sut.TestValidate(Valid() with { Email = new string('a', 244) + "@example.com" });

        // Assert
        atLimit.ShouldNotHaveValidationErrorFor(x => x.Email);
        overLimit.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankUsername_AndEnforceTheLengthBoundary()
    {
        // Act
        var blank = _sut.TestValidate(Valid() with { Username = "" });
        var atLimit = _sut.TestValidate(Valid() with { Username = new string('a', 50) });
        var overLimit = _sut.TestValidate(Valid() with { Username = new string('a', 51) });

        // Assert
        blank.ShouldHaveValidationErrorFor(x => x.Username);
        atLimit.ShouldNotHaveValidationErrorFor(x => x.Username);
        overLimit.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("12345", true)]
    [InlineData("123456", false)]
    public void Validate_Should_RequireAPasswordOfAtLeastSixCharacters(string password, bool invalid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Password = password });

        // Assert
        if (invalid) result.ShouldHaveValidationErrorFor(x => x.Password);
        else result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_Should_Fail_ForAnUndefinedRole()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Role = (UserRole)99 });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
