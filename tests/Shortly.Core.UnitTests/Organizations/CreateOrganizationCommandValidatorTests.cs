using FluentValidation.TestHelper;
using Shortly.Core.Organizations.CreateOrganization;

namespace Shortly.Core.UnitTests.Organizations;

public class CreateOrganizationCommandValidatorTests
{
    private readonly CreateOrganizationCommandValidator _sut = new();

    private static CreateOrganizationCommand Valid() =>
        new("Acme", "Description", "https://acme.dev", "https://acme.dev/logo.png");

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_WhenEveryOptionalFieldIsOmitted()
    {
        // Act
        var result = _sut.TestValidate(new CreateOrganizationCommand("Acme", null, null, null));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_ForABlankName(string name)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Name = name });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validate_Should_EnforceTheNameLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Name = new string('n', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Name);
        else result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Validate_Should_EnforceTheDescriptionLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Description = new string('d', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Description);
        else result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://acme.dev")]
    [InlineData("javascript:alert(1)")]
    public void Validate_Should_Fail_ForAWebsiteOrLogoThatIsNotAnHttpUrl(string url)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Website = url, LogoUrl = url });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Website);
        result.ShouldHaveValidationErrorFor(x => x.LogoUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_IgnoreABlankWebsiteOrLogo(string blank)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Website = blank, LogoUrl = blank });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Website);
        result.ShouldNotHaveValidationErrorFor(x => x.LogoUrl);
    }
}
