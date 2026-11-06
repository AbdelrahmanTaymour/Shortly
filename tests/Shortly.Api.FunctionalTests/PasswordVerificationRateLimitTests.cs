using System.Net;
using System.Net.Http.Json;
using System.Web;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// POST /api/url-redirect/verify is an anonymous online password oracle. It is limited per client
/// (IPv4 address / IPv6 /64) and answers with the password page and 429 + Retry-After when the limit is hit.
/// </summary>
[Collection(ApiCollection.Name)]
public class PasswordVerificationRateLimitTests(ApiFixture api) : ApiTestBase(api)
{
    private const string Password = "correct-password-1";
    private static readonly System.Net.IPAddress Peer = System.Net.IPAddress.Parse("198.51.100.99");

    private static Dictionary<string, string?> Limit(int permits, params (string Key, string Value)[] extra)
    {
        var settings = new Dictionary<string, string?>
        {
            ["RateLimiting:PasswordVerification:PermitLimit"] = permits.ToString(),
            ["RateLimiting:PasswordVerification:WindowSeconds"] = "300"
        };
        foreach (var (key, value) in extra) settings[key] = value;
        return settings;
    }

    private sealed record ProtectedLink(string ShortCode, string OriginalUrl);

    private async Task<ProtectedLink> ProtectedLinkAsync()
    {
        var owner = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(owner, Shortly.Domain.SubscriptionPlan.enSubscriptionPlan.Professional); // link protection is a Professional feature
        var linkId = await Seed.CreateUserLinkAsync(owner);
        (await As(owner).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var link = (await Seed.GetLinkAsync(linkId))!;
        return new ProtectedLink(link.ShortCode, link.OriginalUrl);
    }

    /// <summary>The 5-minute token the password page receives when a protected link is opened.</summary>
    private static async Task<string> PasswordPageTokenAsync(HttpClient client, ProtectedLink link)
    {
        var response = await client.GetAsync($"/{link.ShortCode}");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect, await response.Content.ReadAsStringAsync());
        var location = new Uri(new Uri("https://localhost"), response.Headers.Location!);
        return HttpUtility.ParseQueryString(location.Query)["token"]!;
    }

    private static Task<HttpResponseMessage> Verify(HttpClient client, string token, string password, string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/url-redirect/verify")
        {
            Content = new FormUrlEncodedContent([new("token", token), new("password", password)])
        };
        if (forwardedFor is not null) request.Headers.Add("X-Forwarded-For", forwardedFor);
        return client.SendAsync(request);
    }

    // ---- behavior within the limit ---------------------------------------------------------------------------

    [Fact]
    public async Task CorrectPassword_WithinTheLimit_RedirectsToTheOriginalUrl()
    {
        var link = await ProtectedLinkAsync();
        var client = Api.CreateFactory(Limit(3)).CreateApiClient();

        var response = await Verify(client, await PasswordPageTokenAsync(client, link), Password);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(link.OriginalUrl);
    }

    [Fact]
    public async Task WrongPassword_WithinTheLimit_RedisplaysThePageWithAnError()
    {
        var link = await ProtectedLinkAsync();
        var client = Api.CreateFactory(Limit(3)).CreateApiClient();

        var response = await Verify(client, await PasswordPageTokenAsync(client, link), "wrong-password");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Invalid password");
    }

    // ---- behavior at the limit -------------------------------------------------------------------------------

    [Fact]
    public async Task AfterTheLimit_FurtherAttempts_Get429WithRetryAfterAndTheHtmlPage()
    {
        var link = await ProtectedLinkAsync();
        var client = Api.CreateFactory(Limit(3)).CreateApiClient();
        var token = await PasswordPageTokenAsync(client, link);

        for (var i = 1; i <= 3; i++)
            (await Verify(client, token, "wrong-password")).StatusCode.Should().Be(HttpStatusCode.OK, $"attempt {i}");

        var limited = await Verify(client, token, "wrong-password");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        limited.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        (await limited.Content.ReadAsStringAsync()).Should().Contain("Too many password attempts");
    }

    [Fact]
    public async Task AfterTheLimit_EvenTheCorrectPassword_IsRefused()
    {
        var link = await ProtectedLinkAsync();
        var client = Api.CreateFactory(Limit(2)).CreateApiClient();
        var token = await PasswordPageTokenAsync(client, link);
        await Verify(client, token, "wrong-1");
        await Verify(client, token, "wrong-2");

        var response = await Verify(client, token, Password);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task DefaultConfiguration_AllowsTenAttemptsPerFiveMinutes()
    {
        var link = await ProtectedLinkAsync();
        var client = Api.CreateFactory().CreateApiClient(); // no overrides: the shipped defaults
        var token = await PasswordPageTokenAsync(client, link);

        for (var i = 1; i <= 10; i++)
            (await Verify(client, token, "wrong-password")).StatusCode.Should().Be(HttpStatusCode.OK, $"attempt {i}");

        var limited = await Verify(client, token, "wrong-password");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter!.Delta!.Value.TotalSeconds.Should().BeInRange(1, 300);
    }

    // ---- partitioning ----------------------------------------------------------------------------------------

    [Fact]
    public async Task TheLimitIsPerClient_NotPerLink_AndOtherClientsAreUnaffected()
    {
        var linkA = await ProtectedLinkAsync();
        var linkB = await ProtectedLinkAsync();
        var factory = Api.CreateFactory(Limit(2, ("ForwardedHeaders:KnownProxies:0", Peer.ToString())), Peer);
        var client = factory.CreateApiClient();
        var tokenA = await PasswordPageTokenAsync(client, linkA);
        var tokenB = await PasswordPageTokenAsync(client, linkB);

        // Client 203.0.113.1 spends its attempts on link A ...
        await Verify(client, tokenA, "wrong-1", "203.0.113.1");
        await Verify(client, tokenA, "wrong-2", "203.0.113.1");

        // ... and is then also limited on link B (the per-client trade-off) ...
        (await Verify(client, tokenB, Password, "203.0.113.1")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // ... while a different client is not affected.
        (await Verify(client, tokenB, Password, "203.0.113.2")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task IPv6Clients_InTheSame64_ShareTheLimit()
    {
        var link = await ProtectedLinkAsync();
        var proxy = System.Net.IPAddress.Parse("2001:db8:ffff::1");
        var factory = Api.CreateFactory(Limit(2, ("ForwardedHeaders:KnownProxies:0", proxy.ToString())), proxy);
        var client = factory.CreateApiClient();
        var token = await PasswordPageTokenAsync(client, link);

        await Verify(client, token, "wrong-1", "2001:db8:1:2::a");
        await Verify(client, token, "wrong-2", "2001:db8:1:2::b");

        (await Verify(client, token, "wrong-3", "2001:db8:1:2::c")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await Verify(client, token, Password, "2001:db8:1:3::a")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task UntrustedPeer_CannotEvadeTheLimitWithXForwardedFor()
    {
        var link = await ProtectedLinkAsync();
        var client = Api.CreateFactory(Limit(2), Peer).CreateApiClient(); // peer is not a trusted proxy
        var token = await PasswordPageTokenAsync(client, link);

        await Verify(client, token, "wrong-1", "203.0.113.1");
        await Verify(client, token, "wrong-2", "203.0.113.2");

        (await Verify(client, token, "wrong-3", "203.0.113.3")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OnlyTheVerifyEndpoint_IsLimited_ShowingThePasswordPageIsNot()
    {
        var link = await ProtectedLinkAsync();
        var client = Api.CreateFactory(Limit(1)).CreateApiClient();
        var token = await PasswordPageTokenAsync(client, link);
        await Verify(client, token, "wrong-1");
        (await Verify(client, token, "wrong-2")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        (await client.GetAsync($"/api/url-redirect/password-page?token={Uri.EscapeDataString(token)}")).StatusCode
            .Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/{link.ShortCode}")).StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}
