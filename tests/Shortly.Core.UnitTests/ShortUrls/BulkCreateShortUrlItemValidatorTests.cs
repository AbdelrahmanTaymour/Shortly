using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.BulkCreateShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkCreateShortUrlItemValidatorTests
{
    private readonly BulkCreateShortUrlItemValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_ForAValidItem()
    {
        // Act
        var result = _sut.TestValidate(new BulkCreateShortUrlItem("https://example.com/a", "Title", "Description"));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    public void Validate_Should_Fail_ForABlankOrNonHttpUrl(string url)
    {
        // Act
        var result = _sut.TestValidate(new BulkCreateShortUrlItem(url, null, null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalUrl);
    }

    [Fact]
    public void Validate_Should_Fail_ForAUrlLongerThan2048Characters()
    {
        // Act
        var result = _sut.TestValidate(new BulkCreateShortUrlItem("https://example.com/" + new string('a', 2040), null, null));

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalUrl);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validate_Should_EnforceTheTitleLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(new BulkCreateShortUrlItem("https://example.com", new string('t', length), null));

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Title);
        else result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Validate_Should_EnforceTheDescriptionLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(new BulkCreateShortUrlItem("https://example.com", null, new string('d', length)));

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Description);
        else result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
