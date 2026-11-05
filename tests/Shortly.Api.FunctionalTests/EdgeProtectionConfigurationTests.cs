using System.Net;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Infrastructure.EdgeProtection.Models;
using Shortly.Infrastructure.EdgeProtection.Services;

namespace Shortly.Api.FunctionalTests;

public class ClientIpPartitionKeyTests
{
    private static string Key(string ip) => ClientIpPartitionKey.From(IPAddress.Parse(ip));

    [Fact]
    public void IPv4_UsesTheFullAddress()
    {
        Key("203.0.113.5").Should().Be("203.0.113.5");
        Key("203.0.113.5").Should().NotBe(Key("203.0.113.6"));
    }

    [Fact]
    public void IPv4MappedIPv6_IsTheSameClientAsThePlainIPv4Address() =>
        Key("::ffff:203.0.113.5").Should().Be(Key("203.0.113.5"));

    [Fact]
    public void IPv6_AddressesInTheSame64_ShareAKey() =>
        Key("2001:db8:1:2:aaaa:bbbb:cccc:dddd").Should().Be(Key("2001:db8:1:2::1"));

    [Fact]
    public void IPv6_AddressesInDifferent64s_HaveDifferentKeys()
    {
        Key("2001:db8:1:2::1").Should().NotBe(Key("2001:db8:1:3::1"));
        Key("2001:db8:1:2::1").Should().NotBe(Key("2001:db9:1:2::1"));
    }

    [Fact]
    public void IPv6Key_CannotCollideWithAnIPv4Key() => Key("2001:db8::1").Should().EndWith("/64");

    [Fact]
    public void UnknownAddress_HasAStableKey() => ClientIpPartitionKey.From(null).Should().Be("unknown");
}

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

[Collection(ApiCollection.Name)]
public class ForwardedHeadersStartupTests(ApiFixture api)
{
    [Fact]
    public void ApplicationRefusesToStart_WhenTrustAllProxiesIsCombinedWithAKnownProxy()
    {
        var act = () => api.CreateFactory(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:TrustAllProxies"] = "true",
            ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.5"
        });

        act.Should().Throw<Exception>().Which.ToString().Should().Contain("TrustAllProxies");
    }
}
