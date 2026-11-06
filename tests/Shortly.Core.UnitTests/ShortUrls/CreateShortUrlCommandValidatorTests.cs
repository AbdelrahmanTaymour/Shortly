using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.CreateShortUrl;
using Shortly.Core.UnitTests.TestData;

namespace Shortly.Core.UnitTests.ShortUrls;

public class CreateShortUrlCommandValidatorTests
{
    private readonly CreateShortUrlCommandValidator _sut = new(TestClock.Provider());

    private static CreateShortUrlCommand Valid() =>
        new("https://example.com/page", null, null, null, null, null, null, null);

    [Fact]
    public void Validate_Should_Pass_ForAUrlOnly()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_ForEveryOptionalSetting()
    {
        // Act
        var result = _sut.TestValidate(new CreateShortUrlCommand(
            "https://example.com", "my-code", 10, true, false, TestClock.UtcNow.AddDays(1), "Title", "Description"));

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
        var result = _sut.TestValidate(Valid() with { OriginalUrl = url });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalUrl);
    }

    [Fact]
    public void Validate_Should_Fail_ForAUrlLongerThan2048Characters()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { OriginalUrl = "https://example.com/" + new string('a', 2040) });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OriginalUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_IgnoreABlankCustomCode(string? code)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { CustomShortCode = code });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomShortCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("has space")]
    [InlineData("dot.code")]
    [InlineData("code\n")]
    public void Validate_Should_Fail_ForAMalformedCustomCode(string code)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { CustomShortCode = code });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomShortCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_ForAClickLimitThatIsNotPositive(int clickLimit)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ClickLimit = clickLimit });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ClickLimit);
    }

    [Fact]
    public void Validate_Should_AcceptAnOmittedClickLimit_MeaningUnlimited()
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ClickLimit = null });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ClickLimit);
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
    public void Validate_Should_EnforceTheSameCustomCodeLengthLimitsAsTheDomain(int length, bool valid)
    {
        // Act
        var validatorResult = _sut.TestValidate(Valid() with { CustomShortCode = new string('a', length) });

        // Assert
        if (valid) validatorResult.ShouldNotHaveValidationErrorFor(x => x.CustomShortCode);
        else validatorResult.ShouldHaveValidationErrorFor(x => x.CustomShortCode);
        Shortly.Domain.ShortUrls.ShortCode.Create(new string('a', length)).IsSuccess.Should().Be(valid);
    }
}
