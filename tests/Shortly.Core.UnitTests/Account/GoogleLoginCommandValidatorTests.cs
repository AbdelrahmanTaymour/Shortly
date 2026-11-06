using FluentValidation.TestHelper;
using Shortly.Core.Account.GoogleLogin;

namespace Shortly.Core.UnitTests.Account;

public class GoogleLoginCommandValidatorTests
{
    private readonly GoogleLoginCommandValidator _sut = new();

    private static GoogleLoginCommand Valid() => new("google-123", "jane@example.com", "Jane Doe", "https://example.com/pic.png");

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_WhenTheNameAndPictureAreMissing()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Name = null, Picture = null });

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankGoogleId()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { GoogleId = "" });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GoogleId);
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

    [Theory]
    [InlineData(150, true)]
    [InlineData(151, false)]
    public void Validate_Should_EnforceTheNameLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Name = new string('a', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Name);
        else result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Validate_Should_EnforceThePictureUrlLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Picture = new string('a', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Picture);
        else result.ShouldHaveValidationErrorFor(x => x.Picture);
    }
}
