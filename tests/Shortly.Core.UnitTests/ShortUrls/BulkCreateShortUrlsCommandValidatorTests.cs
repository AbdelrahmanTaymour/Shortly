using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.BulkCreateShortUrls;

namespace Shortly.Core.UnitTests.ShortUrls;

public class BulkCreateShortUrlsCommandValidatorTests
{
    private readonly BulkCreateShortUrlsCommandValidator _sut = new();

    private static BulkCreateShortUrlItem Item(string url = "https://example.com", string? title = null, string? description = null) =>
        new(url, title, description);

    private static BulkCreateShortUrlsCommand Command(params BulkCreateShortUrlItem[] items) => new(items);

    [Fact]
    public void Validate_Should_Pass_ForAFewValidItems()
    {
        // Act
        var result = _sut.TestValidate(Command(Item(), Item("https://example.org/a", "Title", "Description")));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_ForNoItems()
    {
        // Act
        var result = _sut.TestValidate(Command());

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Urls);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Validate_Should_LimitTheBatchTo100Items(int count, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Command(Enumerable.Range(0, count).Select(_ => Item()).ToArray()));

        // Assert
        if (valid) result.ShouldNotHaveAnyValidationErrors();
        else result.ShouldHaveValidationErrorFor(x => x.Urls);
    }

    [Fact]
    public void Validate_Should_ValidateEveryItem()
    {
        // Act
        var result = _sut.TestValidate(Command(Item(), Item("not a url")));

        // Assert
        result.ShouldHaveValidationErrorFor("Urls[1].OriginalUrl");
        result.ShouldNotHaveValidationErrorFor("Urls[0].OriginalUrl");
    }
}
