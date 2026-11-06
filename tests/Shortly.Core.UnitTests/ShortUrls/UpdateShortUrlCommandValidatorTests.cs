using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.UpdateShortUrl;
using Shortly.Core.UnitTests.TestData;

namespace Shortly.Core.UnitTests.ShortUrls;

public class UpdateShortUrlCommandValidatorTests
{
    private readonly UpdateShortUrlCommandValidator _sut = new(TestClock.Provider());

    private static UpdateShortUrlCommand Valid() =>
        new(1, "https://example.com/page", "my-code", "Title", "Description", 10, true, false, true, null);

    [Fact]
    public void Validate_Should_Pass_ForAValidCommand()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAnIdThatIsNotPositive(long id)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ShortUrlId = id });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortUrlId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    public void Validate_Should_Fail_ForABlankOrNonHttpUrl(string url)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { OriginalUrl = url });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("has space")]
    [InlineData("code\n")]
    public void Validate_Should_Fail_ForABlankShortOrMalformedCode(string code)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ShortCode = code });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortCode);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    public void Validate_Should_Fail_ForAnExpirationThatIsNotInTheFuture(int secondsFromNow)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ExpiresAt = TestClock.UtcNow.AddSeconds(secondsFromNow) });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Fact]
    public void Validate_Should_AcceptAClearedExpiration()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ExpiresAt = null });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAt);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Validate_Should_EnforceTheTitleLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { Title = new string('t', length) });

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
        var result = _sut.TestValidate(Valid() with { Description = new string('d', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.Description);
        else result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void Validate_Should_EnforceTheSameCodeLengthLimitsAsTheDomain(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ShortCode = new string('a', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.ShortCode);
        else result.ShouldHaveValidationErrorFor(x => x.ShortCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(-1)]
    public void Validate_Should_AcceptAPositiveClickLimit_OrMinusOneForUnlimited(int clickLimit)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ClickLimit = clickLimit });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ClickLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(int.MinValue)]
    public void Validate_Should_Fail_ForAClickLimitThatIsZeroOrBelowMinusOne(int clickLimit)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ClickLimit = clickLimit });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ClickLimit);
    }
}
