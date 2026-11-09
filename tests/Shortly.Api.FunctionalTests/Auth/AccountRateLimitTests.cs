using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests.Auth;

[Collection(ApiCollection.Name)]
public class AccountRateLimitTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly System.Net.IPAddress Peer = System.Net.IPAddress.Parse("198.51.100.99");

    private static Task<HttpResponseMessage> Register(HttpClient client, string? forwardedFor = null)
    {
        var unique = Guid.NewGuid().ToString("N");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new { email = $"{unique}@example.com", username = $"u{unique}"[..20], password = "Str0ng-password!" })
        };
        if (forwardedFor is not null) request.Headers.Add("X-Forwarded-For", forwardedFor);
        return client.SendAsync(request);
    }

    private static Dictionary<string, string?> Limit(string section, int permits, params (string Key, string Value)[] extra)
    {
        var settings = new Dictionary<string, string?>
        {
            [$"RateLimiting:{section}:PermitLimit"] = permits.ToString(),
            [$"RateLimiting:{section}:WindowSeconds"] = "3600"
        };
        foreach (var (key, value) in extra) settings[key] = value;
        return settings;
    }

    // ---- registration ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Registration_IsLimitedPerClient_With429AndRetryAfter()
    {
        var client = Api.CreateFactory(Limit("Registration", 2)).CreateApiClient();

        (await Register(client)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Register(client)).StatusCode.Should().Be(HttpStatusCode.OK);

        var limited = await Register(client);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        (await ReadJsonAsync(limited)).GetProperty("detail").GetString().Should().NotContain("Anonymous short URL");
    }

    [Fact]
    public async Task Registration_DefaultLimit_IsTenPerHour()
    {
        var client = Api.CreateFactory().CreateApiClient();      // shipped defaults

        for (var i = 1; i <= 10; i++)
            (await Register(client)).StatusCode.Should().Be(HttpStatusCode.OK, $"registration {i}");

        (await Register(client)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Registration_UsesTheClientAddressBehindATrustedProxy_AndIgnoresSpoofedHeadersOtherwise()
    {
        var trusted = Api.CreateFactory(Limit("Registration", 1, ("ForwardedHeaders:KnownProxies:0", Peer.ToString())), Peer).CreateApiClient();
        (await Register(trusted, "203.0.113.1")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Register(trusted, "203.0.113.2")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Register(trusted, "203.0.113.1")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var untrusted = Api.CreateFactory(Limit("Registration", 1), Peer).CreateApiClient();
        (await Register(untrusted, "203.0.113.1")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Register(untrusted, "203.0.113.2")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Registration_LimitDoesNotAffectLogin()
    {
        var factory = Api.CreateFactory(Limit("Registration", 1));
        var client = factory.CreateApiClient();
        await Register(client);
        (await Register(client)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Login is a different endpoint with its own protection (account lockout): the registration limiter must not touch it.
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong-password-1" });
        login.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    // ---- short-code availability ---------------------------------------------------------------------------------------

    [Fact]
    public async Task CodeAvailability_IsLimitedPerClient_AcrossUsers()
    {
        var factory = Api.CreateFactory(Limit("ShortCodeAvailability", 3));
        var userA = factory.CreateApiClient(TestTokens.Create(await Seed.CreateUserAsync()));
        var userB = factory.CreateApiClient(TestTokens.Create(await Seed.CreateUserAsync()));

        for (var i = 0; i < 3; i++)
            (await userA.GetAsync($"/api/short-urls/code-available?shortCode=free{i}code")).StatusCode.Should().Be(HttpStatusCode.OK);

        var limited = await userA.GetAsync("/api/short-urls/code-available?shortCode=free9code");
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();

        // The limit is per client address, so a second account behind the same address shares it.
        (await userB.GetAsync("/api/short-urls/code-available?shortCode=free8code")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task CodeAvailability_DefaultLimit_Allows60PerMinute_AndStillAnswersCorrectly()
    {
        var factory = Api.CreateFactory();
        var user = await Seed.CreateUserAsync();
        var existing = (await Seed.GetLinkAsync(await Seed.CreateUserLinkAsync(user)))!.ShortCode;
        var client = factory.CreateApiClient(TestTokens.Create(user));

        (await ReadBoolAsync(await client.GetAsync($"/api/short-urls/code-available?shortCode={existing}"))).Should().BeFalse();
        (await ReadBoolAsync(await client.GetAsync($"/api/short-urls/code-available?shortCode=brandnew{Guid.NewGuid():N}"[..58]))).Should().BeTrue();

        for (var i = 0; i < 58; i++) await client.GetAsync("/api/short-urls/code-available?shortCode=filler1234");
        (await client.GetAsync("/api/short-urls/code-available?shortCode=filler1234")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task CodeAvailability_StillRequiresAuthentication()
    {
        (await Anonymous().GetAsync("/api/short-urls/code-available?shortCode=anything")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<bool> ReadBoolAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<bool>();
    }
}
