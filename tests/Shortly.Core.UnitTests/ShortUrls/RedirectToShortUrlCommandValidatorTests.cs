using FluentValidation.TestHelper;
using Shortly.Core.ShortUrls.RedirectToShortUrl;

namespace Shortly.Core.UnitTests.ShortUrls;

public class RedirectToShortUrlCommandValidatorTests
{
    private readonly RedirectToShortUrlCommandValidator _sut = new();

    private static RedirectToShortUrlCommand Valid() => new(
        "abc1234", null, "203.0.113.1", "session", "Mozilla/5.0", null, null, null, null, null, null);

    [Fact]
    public void Validate_Should_Pass_ForARegularRequest()
    {
        // Act
        var result = _sut.TestValidate(Valid());

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_TreatTheUserAgentAsOptional()
    {
        // Act: some clients send no User-Agent at all
        var result = _sut.TestValidate(Valid() with { UserAgent = "" });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserAgent);
    }

    [Theory]
    [InlineData(2048, true)]
    [InlineData(2049, false)]
    public void Validate_Should_EnforceTheUserAgentAndReferrerLengths(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { UserAgent = new string('u', length), Referrer = new string('r', length) });

        // Assert
        if (valid) result.ShouldNotHaveAnyValidationErrors();
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.UserAgent);
            result.ShouldHaveValidationErrorFor(x => x.Referrer);
        }
    }

    [Theory]
    [InlineData("")]
    public void Validate_Should_Fail_ForABlankShortCode(string code)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ShortCode = code });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortCode);
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void Validate_Should_EnforceTheShortCodeLength(int length, bool valid)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { ShortCode = new string('a', length) });

        // Assert
        if (valid) result.ShouldNotHaveValidationErrorFor(x => x.ShortCode);
        else result.ShouldHaveValidationErrorFor(x => x.ShortCode);
    }

    [Theory]
    [InlineData("203.0.113.1")]
    [InlineData("2001:db8::1")]
    public void Validate_Should_AcceptIpv4AndIpv6Addresses(string ip)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { IpAddress = ip });

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IpAddress);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("999.1.1.1")]
    public void Validate_Should_Fail_ForABlankOrInvalidIpAddress(string ip)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { IpAddress = ip });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IpAddress);
    }

    [Theory]
    [InlineData("")]
    public void Validate_Should_Fail_ForABlankSessionId(string sessionId)
    {
        // Act
        var result = _sut.TestValidate(Valid() with { SessionId = sessionId });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SessionId);
    }

    [Fact]
    public void Validate_Should_EnforceThePasswordAndCampaignLengths()
    {
        // Act
        var result = _sut.TestValidate(Valid() with
        {
            Password = new string('p', 129), UtmSource = new string('s', 256), UtmMedium = new string('m', 256),
            UtmCampaign = new string('c', 256), UtmTerm = new string('t', 256), UtmContent = new string('n', 256)
        });

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
        result.ShouldHaveValidationErrorFor(x => x.UtmSource);
        result.ShouldHaveValidationErrorFor(x => x.UtmMedium);
        result.ShouldHaveValidationErrorFor(x => x.UtmCampaign);
        result.ShouldHaveValidationErrorFor(x => x.UtmTerm);
        result.ShouldHaveValidationErrorFor(x => x.UtmContent);
    }
}
