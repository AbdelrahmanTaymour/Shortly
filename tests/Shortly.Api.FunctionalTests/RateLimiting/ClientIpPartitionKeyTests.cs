using System.Net;
using Shortly.Infrastructure.EdgeProtection.Services;

namespace Shortly.Api.FunctionalTests.RateLimiting;

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
