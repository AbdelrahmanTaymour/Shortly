using Shortly.Domain.ShortUrls;

namespace Shortly.Domain.UnitTests.ShortUrls;

public class ShortCodeTests
{
    [Theory]
    [InlineData("abcd")]
    [InlineData("Ab1_-xyz")]
    [InlineData("12345678901234567890")]
    public void Create_Should_Succeed_When_CodeIsValid(string value)
    {
        // Act
        var result = ShortCode.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Create_Should_Fail_When_CodeIsEmpty(string? value)
    {
        // Act
        var result = ShortCode.Create(value!);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeEmpty);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123456789012345678901")]
    public void Create_Should_Fail_When_LengthIsOutOfRange(string value)
    {
        // Act
        var result = ShortCode.Create(value);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeInvalidLength);
    }

    [Theory]
    [InlineData("abc d")]
    [InlineData("abc/def")]
    [InlineData("abc.def")]
    [InlineData("abc@def")]
    [InlineData("héllo")]
    public void Create_Should_Fail_When_CodeHasInvalidCharacters(string value)
    {
        // Act
        var result = ShortCode.Create(value);

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeInvalidCharacters);
    }

    [Fact]
    public void Create_Should_CheckLengthBeforeCharacters()
    {
        // Act: too short AND invalid characters
        var result = ShortCode.Create("a/");

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeInvalidLength);
    }

    [Theory]
    [InlineData("abcd\n")]
    [InlineData("abcd\r\n")]
    [InlineData("\nabcd")]
    public void Create_Should_Fail_When_CodeContainsNewlines(string value)
    {
        // Act
        var result = ShortCode.Create(value);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ShortUrlErrors.ShortCodeInvalidCharacters);
    }

    [Fact]
    public void Codes_Should_BeCaseSensitive()
    {
        // Arrange
        var lower = ShortCode.Create("abcd").Value;
        var upper = ShortCode.Create("ABCD").Value;

        // Assert
        lower.Should().NotBe(upper);
    }

    [Fact]
    public void ToString_Should_ReturnTheValue()
    {
        // Arrange
        var code = ShortCode.Create("abcd").Value;

        // Act
        var text = code.ToString();

        // Assert
        text.Should().Be("abcd");
    }
}
