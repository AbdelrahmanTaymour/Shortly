using FluentValidation.TestHelper;
using Shortly.Core.Organizations.UpdateOrganization;

namespace Shortly.Core.UnitTests.Organizations;

public class UpdateOrganizationCommandValidatorTests
{
    private readonly UpdateOrganizationCommandValidator _sut = new();

    private static UpdateOrganizationCommand Valid() =>
        new(Guid.NewGuid(), "Acme", "Description", "https://acme.dev", "https://acme.dev/logo.png");

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_WhenEveryFieldExceptTheIdIsOmitted()
    {
        // Act
        var result = _sut.TestValidate(new UpdateOrganizationCommand(Guid.NewGuid(), null, null, null, null));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForABlankOrganizationId()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { OrganizationId = Guid.Empty });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
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
    public void Validate_Should_AcceptABlankWebsiteOrLogo_BecauseThatClearsTheField(string blank)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Website = blank, LogoUrl = blank });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Website);
        result.ShouldNotHaveValidationErrorFor(x => x.LogoUrl);
    }
}
