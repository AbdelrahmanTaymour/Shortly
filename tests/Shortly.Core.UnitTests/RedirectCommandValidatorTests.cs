using Shortly.Core.ShortUrls.RedirectToShortUrl;

namespace Shortly.Core.UnitTests;

public class RedirectCommandValidatorTests
{
    private readonly RedirectToShortUrlCommandValidator _validator = new();

    private static RedirectToShortUrlCommand Command(string userAgent = "Mozilla/5.0") => new(
        "abc1234", null, "203.0.113.1", "session", userAgent, null, null, null, null, null, null);

    [Fact]
    public void UserAgent_IsOptional() => _validator.Validate(Command(userAgent: "")).IsValid.Should().BeTrue();

    [Fact]
    public void UserAgent_UpToTheValidatorLimit_IsValid() =>
        _validator.Validate(Command(userAgent: new string('u', 2048))).IsValid.Should().BeTrue();

    [Fact]
    public void ARegularRequest_IsValid() => _validator.Validate(Command()).IsValid.Should().BeTrue();

    [Fact]
    public void OtherRequiredFields_AreStillEnforced()
    {
        var withoutSession = Command() with { SessionId = "" };
        var withBadIp = Command() with { IpAddress = "not-an-ip" };
        var withoutCode = Command() with { ShortCode = "" };

        _validator.Validate(withoutSession).IsValid.Should().BeFalse();
        _validator.Validate(withBadIp).IsValid.Should().BeFalse();
        _validator.Validate(withoutCode).IsValid.Should().BeFalse();
    }
}
