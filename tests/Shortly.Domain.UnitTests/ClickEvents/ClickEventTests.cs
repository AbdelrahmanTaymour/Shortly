using Shortly.Domain.ClickEvents;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.ClickEvents;

public class ClickEventTests
{
    private static Shortly.Domain.Abstractions.Result<ClickEvent> TryRecord(
        long shortUrlId = 42,
        string ipAddress = "203.0.113.5",
        string sessionId = "session-1",
        string userAgent = "Mozilla/5.0",
        string? referrer = "https://news.example.com/post",
        string? utmSource = "newsletter") =>
        ClickEvent.Record(shortUrlId, TestClock.Now, ipAddress, sessionId, userAgent,
            referrer, "news.example.com", "referral",
            utmSource, "email", "spring-sale", "shoes", "banner",
            "Germany", "Berlin", "Chrome", "macOS", "Desktop", "Desktop");

    private static ClickEvent Record() => TryRecord().Value;

    [Fact]
    public void Record_Should_CaptureEveryDetailOfTheClick()
    {
        // Act
        var result = TryRecord();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var click = result.Value;
        click.Id.Should().NotBeEmpty();
        click.ShortUrlId.Should().Be(42);
        click.ClickedAtUtc.Should().Be(TestClock.Now);
        click.IpAddress.Should().Be("203.0.113.5");
        click.SessionId.Should().Be("session-1");
        click.UserAgent.Should().Be("Mozilla/5.0");
        click.Referrer.Should().Be("https://news.example.com/post");
        click.ReferrerDomain.Should().Be("news.example.com");
        click.TrafficSource.Should().Be("referral");
        click.UtmSource.Should().Be("newsletter");
        click.UtmMedium.Should().Be("email");
        click.UtmCampaign.Should().Be("spring-sale");
        click.UtmTerm.Should().Be("shoes");
        click.UtmContent.Should().Be("banner");
        click.Country.Should().Be("Germany");
        click.City.Should().Be("Berlin");
        click.Browser.Should().Be("Chrome");
        click.OperatingSystem.Should().Be("macOS");
        click.Device.Should().Be("Desktop");
        click.DeviceType.Should().Be("Desktop");
    }

    [Fact]
    public void Record_Should_AllowMissingReferrerAndCampaignDetails()
    {
        // Act
        var click = ClickEvent.Record(1, TestClock.Now, "203.0.113.5", "session-1", "Mozilla/5.0",
            null, null, null, null, null, null, null, null,
            "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", null).Value;

        // Assert
        click.Referrer.Should().BeNull();
        click.ReferrerDomain.Should().BeNull();
        click.TrafficSource.Should().BeNull();
        click.UtmSource.Should().BeNull();
        click.UtmMedium.Should().BeNull();
        click.UtmCampaign.Should().BeNull();
        click.UtmTerm.Should().BeNull();
        click.UtmContent.Should().BeNull();
        click.DeviceType.Should().BeNull();
    }

    [Fact]
    public void Record_Should_GenerateADifferentIdForEveryClick()
    {
        // Act
        var first = Record();
        var second = Record();

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Record_Should_Fail_When_TheShortUrlIdIsNotPositive(long shortUrlId)
    {
        // Act
        var result = TryRecord(shortUrlId: shortUrlId);

        // Assert
        result.Error.Should().Be(ClickEventErrors.InvalidShortUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_Should_Fail_When_TheSessionIdIsBlank(string? sessionId)
    {
        // Act
        var result = TryRecord(sessionId: sessionId!);

        // Assert
        result.Error.Should().Be(ClickEventErrors.SessionRequired);
    }

    [Fact]
    public void Record_Should_Accept_BlankIpAddressAndUserAgent()
    {
        // Act: the redirect pipeline may have nothing to report for these
        var result = TryRecord(ipAddress: "", userAgent: "");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IpAddress.Should().BeEmpty();
        result.Value.UserAgent.Should().BeEmpty();
    }
}
