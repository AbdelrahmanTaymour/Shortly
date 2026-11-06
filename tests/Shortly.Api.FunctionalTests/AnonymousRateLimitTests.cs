using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// Phase 0.6: anonymous short URL creation is rate limited per client IP using the built-in in-memory limiter.
/// Each test uses its own host so limiter state never leaks between tests.
/// </summary>
[Collection(ApiCollection.Name)]
public class AnonymousRateLimitTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly object Body = new { originalUrl = "https://example.com/rate-limit" };

    private static Task<HttpResponseMessage> Create(HttpClient client, string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/short-urls") { Content = JsonContent.Create(Body) };
        if (forwardedFor is not null) request.Headers.Add("X-Forwarded-For", forwardedFor);
        return client.SendAsync(request);
    }

    private static Dictionary<string, string?> Limit(int permits, params (string Key, string Value)[] extra)
    {
        var settings = new Dictionary<string, string?>
        {
            ["RateLimiting:AnonymousShortUrlCreation:PermitLimit"] = permits.ToString(),
            ["RateLimiting:AnonymousShortUrlCreation:WindowSeconds"] = "60"
        };
        foreach (var (key, value) in extra) settings[key] = value;
        return settings;
    }

    [Fact]
    public async Task DefaultConfiguration_AllowsTenAnonymousCreationsPerMinute()
    {
        // No overrides: proves the shipped appsettings.json default of 10 / 60s is what is enforced.
        var factory = Api.CreateFactory();
        var client = factory.CreateApiClient();

        for (var i = 1; i <= 10; i++)
            (await Create(client)).StatusCode.Should().Be(HttpStatusCode.Created, $"request {i} is within the limit");

        var limited = await Create(client);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task LimitIsConfigurable()
    {
        var client = Api.CreateFactory(Limit(permits: 2)).CreateApiClient();

        (await Create(client)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RejectedRequest_ReturnsProblemDetailsAndCreatesNothing()
    {
        var client = Api.CreateFactory(Limit(permits: 1)).CreateApiClient();
        await Create(client);

        var response = await Create(client);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await ReadJsonAsync(response)).GetProperty("status").GetInt32().Should().Be(429);
    }

    [Fact]
    public async Task AuthenticatedUsers_AreNotSubjectToTheAnonymousLimit()
    {
        var factory = Api.CreateFactory(Limit(permits: 1));
        var userId = await Seed.CreateUserAsync();
        var client = factory.CreateApiClient(TestTokens.Create(userId));

        for (var i = 0; i < 5; i++)
            (await Create(client)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ExhaustedAnonymousLimit_DoesNotBlockAuthenticatedUsers()
    {
        var factory = Api.CreateFactory(Limit(permits: 1));
        var anonymous = factory.CreateApiClient();
        await Create(anonymous);
        (await Create(anonymous)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var user = factory.CreateApiClient(TestTokens.Create(await Seed.CreateUserAsync()));

        (await Create(user)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // The TCP peer is a public address (not loopback), as it would be on a real host.
    private static readonly System.Net.IPAddress Peer = System.Net.IPAddress.Parse("198.51.100.99");

    [Fact]
    public async Task UntrustedPeer_CannotUseXForwardedForToEvadeTheLimit()
    {
        // Default configuration trusts no proxy besides loopback, so the client-supplied header is ignored.
        var client = Api.CreateFactory(Limit(permits: 2), Peer).CreateApiClient();

        (await Create(client, "203.0.113.1")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.2")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.3")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TrustedProxy_ByAddress_EachClientIpHasItsOwnBucket()
    {
        var factory = Api.CreateFactory(Limit(permits: 1, ("ForwardedHeaders:KnownProxies:0", Peer.ToString())), Peer);
        var client = factory.CreateApiClient();

        (await Create(client, "203.0.113.10")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.11")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.10")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests); // same client IP again
    }

    [Fact]
    public async Task TrustedProxy_ByNetwork_EachClientIpHasItsOwnBucket()
    {
        var factory = Api.CreateFactory(Limit(permits: 1, ("ForwardedHeaders:KnownNetworks:0", "198.51.100.0/24")), Peer);
        var client = factory.CreateApiClient();

        (await Create(client, "203.0.113.20")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.21")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.20")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TrustAllProxies_HostedBehindPlatformProxy_EachClientIpHasItsOwnBucket()
    {
        var factory = Api.CreateFactory(Limit(permits: 1, ("ForwardedHeaders:TrustAllProxies", "true")), Peer);
        var client = factory.CreateApiClient();

        (await Create(client, "203.0.113.30")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.31")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "203.0.113.30")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TrustAllProxies_OnlyTheEntryAppendedByTheProxyIsUsed()
    {
        // ForwardLimit = 1: a client-spoofed leading X-Forwarded-For entry cannot change the IP the proxy reported.
        var factory = Api.CreateFactory(Limit(permits: 1, ("ForwardedHeaders:TrustAllProxies", "true")), Peer);
        var client = factory.CreateApiClient();

        (await Create(client, "198.51.100.7, 203.0.113.50")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "198.51.100.8, 203.0.113.50")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ---- IPv6 /64 grouping behind a trusted proxy -----------------------------------------------------------

    [Fact]
    public async Task IPv6Clients_InTheSame64_ShareABucket_ButOtherPrefixesDoNot()
    {
        var proxy = System.Net.IPAddress.Parse("2001:db8:ffff::1");
        var factory = Api.CreateFactory(Limit(permits: 1, ("ForwardedHeaders:KnownProxies:0", proxy.ToString())), proxy);
        var client = factory.CreateApiClient();

        (await Create(client, "2001:db8:1:2::a")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "2001:db8:1:2:ffff:ffff:ffff:ffff")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await Create(client, "2001:db8:1:3::a")).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task IPv4MappedIPv6_IsTheSameClientAsPlainIPv4()
    {
        var factory = Api.CreateFactory(Limit(permits: 1, ("ForwardedHeaders:KnownProxies:0", Peer.ToString())), Peer);
        var client = factory.CreateApiClient();

        (await Create(client, "203.0.113.77")).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Create(client, "::ffff:203.0.113.77")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
