using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests.RateLimiting;

[Collection(ApiCollection.Name)]
public class EmailRateLimitTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly IPAddress Peer = IPAddress.Parse("198.51.100.99");

    private static Dictionary<string, string?> Limit(int permits, params (string Key, string Value)[] extra)
    {
        var settings = new Dictionary<string, string?>
        {
            ["RateLimiting:AccountEmail:PermitLimit"] = permits.ToString(),
            ["RateLimiting:AccountEmail:WindowSeconds"] = "3600"
        };
        foreach (var (key, value) in extra) settings[key] = value;
        return settings;
    }

    private static Task<HttpResponseMessage> ForgotPassword(HttpClient client, string? forwardedFor = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/forgot-password")
        {
            Content = JsonContent.Create(new { email = $"{Guid.NewGuid():N}@example.com" })
        };
        if (forwardedFor is not null) request.Headers.Add("X-Forwarded-For", forwardedFor);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task ForgotPassword_IsLimitedPerClient_With429AndRetryAfter()
    {
        var client = Api.CreateFactory(Limit(2)).CreateApiClient();

        (await ForgotPassword(client)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        (await ForgotPassword(client)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

        var limited = await ForgotPassword(client);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task TheThreeEmailEndpoints_ShareOneBudgetPerClient()
    {
        var factory = Api.CreateFactory(Limit(3));
        var user = await Seed.CreateUserAsync();
        var client = factory.CreateApiClient(TestTokens.Create(user));

        (await ForgotPassword(client)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        (await client.PostAsJsonAsync("/api/account/send-email-verification", new { email = (string?)null })).StatusCode
            .Should().NotBe(HttpStatusCode.TooManyRequests);
        (await client.PostAsJsonAsync("/api/account/change-email", new { newEmail = "new@example.com", password = "whatever-password" })).StatusCode
            .Should().NotBe(HttpStatusCode.TooManyRequests);

        // The budget is spent across all three, whichever is called next.
        (await client.PostAsJsonAsync("/api/account/send-email-verification", new { email = (string?)null })).StatusCode
            .Should().Be(HttpStatusCode.TooManyRequests);
        (await client.PostAsJsonAsync("/api/account/change-email", new { newEmail = "new2@example.com", password = "whatever-password" })).StatusCode
            .Should().Be(HttpStatusCode.TooManyRequests);
        (await ForgotPassword(client)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task EmailLimit_DefaultIsFivePerHour_AndTokenEndpointsAreNotAffected()
    {
        var client = Api.CreateFactory().CreateApiClient();      // shipped defaults

        for (var i = 1; i <= 5; i++)
            (await ForgotPassword(client)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"request {i}");
        (await ForgotPassword(client)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Endpoints that only consume an emailed token do not send mail and are not part of this budget.
        (await client.PostAsJsonAsync("/api/account/validate-reset-token", new { token = "unknown-token" })).StatusCode
            .Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task EmailLimit_UsesTheClientAddressBehindATrustedProxy_AndIgnoresSpoofedHeadersOtherwise()
    {
        var trusted = Api.CreateFactory(Limit(1, ("ForwardedHeaders:KnownProxies:0", Peer.ToString())), Peer).CreateApiClient();
        (await ForgotPassword(trusted, "203.0.113.1")).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        (await ForgotPassword(trusted, "203.0.113.2")).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        (await ForgotPassword(trusted, "203.0.113.1")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var untrusted = Api.CreateFactory(Limit(1), Peer).CreateApiClient();
        (await ForgotPassword(untrusted, "203.0.113.1")).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        (await ForgotPassword(untrusted, "203.0.113.2")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}