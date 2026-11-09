using Shortly.Infrastructure.EdgeProtection.Models;

namespace Shortly.Api.FunctionalTests.RateLimiting;

public class ForwardedHeadersSettingsTests
{
    [Fact]
    public void Defaults_AreValid_AndTrustNothingBeyondLoopback()
    {
        var settings = new ForwardedHeadersSettings();

        settings.Validate();

        settings.TrustAllProxies.Should().BeFalse();
        settings.KnownProxies.Should().BeEmpty();
        settings.KnownNetworks.Should().BeEmpty();
        settings.ForwardLimit.Should().Be(1);
    }

    [Fact]
    public void TrustAllProxies_Alone_IsValid() => new ForwardedHeadersSettings { TrustAllProxies = true }.Validate();

    [Fact]
    public void KnownProxiesAndNetworks_WithoutTrustAll_AreValid() =>
        new ForwardedHeadersSettings { KnownProxies = ["10.0.0.5", "2001:db8::1"], KnownNetworks = ["10.0.0.0/8", "2001:db8::/32"] }
            .Validate();

    [Theory]
    [InlineData("proxy")]
    [InlineData("network")]
    public void TrustAllProxies_CombinedWithAnExplicitList_IsRejected(string list)
    {
        var settings = list == "proxy"
            ? new ForwardedHeadersSettings { TrustAllProxies = true, KnownProxies = ["10.0.0.5"] }
            : new ForwardedHeadersSettings { TrustAllProxies = true, KnownNetworks = ["10.0.0.0/8"] };

        var act = settings.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*TrustAllProxies*cannot be combined*");
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("")]
    public void InvalidProxyAddress_IsRejectedWithAClearMessage(string value)
    {
        var act = new ForwardedHeadersSettings { KnownProxies = [value] }.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*KnownProxies*invalid IP address*");
    }

    [Theory]
    [InlineData("10.0.0.0")]
    [InlineData("10.0.0.0/33")]
    [InlineData("10.0.0.0/-1")]
    [InlineData("10.0.0.0/abc")]
    [InlineData("nope/8")]
    [InlineData("2001:db8::/129")]
    public void InvalidNetwork_IsRejectedWithAClearMessage(string value)
    {
        var act = new ForwardedHeadersSettings { KnownNetworks = [value] }.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*KnownNetworks*invalid network*");
    }

    [Fact]
    public void ForwardLimitBelowOne_IsRejected()
    {
        var act = new ForwardedHeadersSettings { ForwardLimit = 0 }.Validate;

        act.Should().Throw<InvalidOperationException>().WithMessage("*ForwardLimit*");
    }
}