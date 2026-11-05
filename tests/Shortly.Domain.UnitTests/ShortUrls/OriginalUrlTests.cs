using Shortly.Domain.ShortUrls;

namespace Shortly.Domain.UnitTests.ShortUrls;

public class OriginalUrlTests
{
    [Theory]
    [InlineData("https://example.com/page")]
    [InlineData("http://example.com/page?x=1&y=2#top")]
    [InlineData("https://sub.example.co.uk:8443/a/b")]
    public void Create_Should_Succeed_When_UrlIsAbsoluteHttpOrHttps(string value)
    {
        // Act
        var result = OriginalUrl.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_UrlIsEmpty(string? value)
    {
        // Act
        var result = OriginalUrl.Create(value!);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.OriginalUrlEmpty);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:user@example.com")]
    [InlineData("file:///etc/passwd")]
    public void Create_Should_Fail_When_UrlIsNotAbsoluteHttpOrHttps(string value)
    {
        // Act
        var result = OriginalUrl.Create(value);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.OriginalUrlInvalid);
    }

    [Fact]
    public void Create_Should_Succeed_When_UrlIsExactlyMaxLength()
    {
        // Arrange
        var value = "https://example.com/" + new string('a', 2048 - "https://example.com/".Length);

        // Act
        var result = OriginalUrl.Create(value);

        // Assert
        value.Length.Should().Be(2048);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_When_UrlExceedsMaxLength()
    {
        // Arrange
        var value = "https://example.com/" + new string('a', 2049 - "https://example.com/".Length);

        // Act
        var result = OriginalUrl.Create(value);

        // Assert
        value.Length.Should().Be(2049);
        result.Error.Should().Be(ShortUrlErrors.OriginalUrlTooLong);
    }

    [Fact]
    public void Create_Should_Fail_When_TheNormalizedUrlExceedsMaxLength()
    {
        // Arrange: 2048 characters as typed, but normalization appends a '/' after the host
        var value = "https://example.com?q=" + new string('a', 2048 - "https://example.com?q=".Length);

        // Act
        var result = OriginalUrl.Create(value);

        // Assert
        value.Length.Should().Be(2048);
        result.Error.Should().Be(ShortUrlErrors.OriginalUrlTooLong);
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com/")]
    [InlineData("HTTPS://EXAMPLE.COM/Path", "https://example.com/Path")]
    public void Create_Should_StoreTheNormalizedUri(string input, string expected)
    {
        // Act
        var result = OriginalUrl.Create(input);

        // Assert: the stored value is the normalized Uri.ToString(), not the text the caller supplied
        result.Value.Value.Should().Be(expected);
    }

    [Fact]
    public void ToString_Should_ReturnTheValue()
    {
        // Arrange
        var url = OriginalUrl.Create("https://example.com/page").Value;

        // Act
        var text = url.ToString();

        // Assert
        text.Should().Be("https://example.com/page");
    }

    [Fact]
    public void Urls_Should_BeEqual_When_ValuesMatch()
    {
        // Arrange
        var first = OriginalUrl.Create("https://example.com/page").Value;
        var second = OriginalUrl.Create("https://example.com/page").Value;

        // Assert
        first.Should().Be(second);
    }
}
