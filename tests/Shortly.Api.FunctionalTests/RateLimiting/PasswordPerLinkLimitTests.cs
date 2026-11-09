using System.Net;
using System.Net.Http.Json;
using System.Web;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Infrastructure.EdgeProtection.Models;

namespace Shortly.Api.FunctionalTests.RateLimiting;

[Collection(ApiCollection.Name)]
public class PasswordPerLinkLimitTests(ApiFixture api) : ApiTestBase(api)
{
    private const string Password = "per-link-password-1";
    private static readonly System.Net.IPAddress Peer = System.Net.IPAddress.Parse("198.51.100.99");

    private ShortlyApiFactory Host(int perLink, int windowSeconds = 3600) => Api.CreateFactory(new Dictionary<string, string?>
    {
        ["RateLimiting:PasswordVerification:PermitLimit"] = "100000",
        ["RateLimiting:PasswordVerificationPerLink:PermitLimit"] = perLink.ToString(),
        ["RateLimiting:PasswordVerificationPerLink:WindowSeconds"] = windowSeconds.ToString(),
        ["ForwardedHeaders:KnownProxies:0"] = Peer.ToString()
    }, Peer);

    private async Task<(string ShortCode, string OriginalUrl)> ProtectedLinkAsync()
    {
        var owner = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(owner, enSubscriptionPlan.Professional);
        var linkId = await Seed.CreateUserLinkAsync(owner);
        (await As(owner).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var link = (await Seed.GetLinkAsync(linkId))!;
        return (link.ShortCode, link.OriginalUrl);
    }

    private static async Task<string> TokenAsync(HttpClient client, string shortCode)
    {
        var page = await client.GetAsync($"/{shortCode}");
        return HttpUtility.ParseQueryString(new Uri(new Uri("https://localhost"), page.Headers.Location!).Query)["token"]!;
    }

    private static Task<HttpResponseMessage> Verify(HttpClient client, string token, string password, string clientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/url-redirect/verify")
        {
            Content = new FormUrlEncodedContent([new("token", token), new("password", password)])
        };
        request.Headers.Add("X-Forwarded-For", clientIp);
        return client.SendAsync(request);
    }

    [Fact]
    public async Task WrongPasswordsFromManyClients_ExhaustTheLinksBudget_AndEvenTheCorrectPasswordIsThenRefused()
    {
        var (code, _) = await ProtectedLinkAsync();
        var client = Host(perLink: 3).CreateApiClient();
        var token = await TokenAsync(client, code);

        for (var i = 1; i <= 3; i++)
            (await Verify(client, token, "wrong", $"203.0.113.{i}")).StatusCode.Should().Be(HttpStatusCode.OK, $"wrong attempt {i}");

        var locked = await Verify(client, token, Password, "203.0.113.200");      // a fresh client, correct password

        locked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        locked.Headers.RetryAfter.Should().NotBeNull();
        locked.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        (await locked.Content.ReadAsStringAsync()).Should().Contain("Too many failed password attempts");
    }

    [Fact]
    public async Task OnlyWrongPasswordsCount_CorrectOnesNeverSpendTheBudget()
    {
        var (code, original) = await ProtectedLinkAsync();
        var client = Host(perLink: 3).CreateApiClient();
        var token = await TokenAsync(client, code);

        for (var i = 1; i <= 10; i++)
        {
            var ok = await Verify(client, token, Password, $"203.0.113.{i}");
            ok.StatusCode.Should().Be(HttpStatusCode.Redirect, $"correct attempt {i}");
            ok.Headers.Location!.ToString().Should().Be(original);
        }

        // Two wrong attempts so far this window: one more is still allowed, then the link locks.
        await Verify(client, token, "wrong", "203.0.113.50");
        await Verify(client, token, "wrong", "203.0.113.51");
        (await Verify(client, token, Password, "203.0.113.52")).StatusCode.Should().Be(HttpStatusCode.Redirect);
        await Verify(client, token, "wrong", "203.0.113.53");
        (await Verify(client, token, Password, "203.0.113.54")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task OtherLinks_AreUnaffectedByALockedLink()
    {
        var (lockedCode, _) = await ProtectedLinkAsync();
        var (otherCode, otherUrl) = await ProtectedLinkAsync();
        var client = Host(perLink: 2).CreateApiClient();
        var lockedToken = await TokenAsync(client, lockedCode);
        var otherToken = await TokenAsync(client, otherCode);
        await Verify(client, lockedToken, "wrong", "203.0.113.1");
        await Verify(client, lockedToken, "wrong", "203.0.113.2");
        (await Verify(client, lockedToken, Password, "203.0.113.3")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var other = await Verify(client, otherToken, Password, "203.0.113.3");

        other.StatusCode.Should().Be(HttpStatusCode.Redirect);
        other.Headers.Location!.ToString().Should().Be(otherUrl);
    }

    [Fact]
    public async Task TheLock_ExpiresWhenTheWindowPasses()
    {
        var (code, original) = await ProtectedLinkAsync();
        var client = Host(perLink: 2, windowSeconds: 2).CreateApiClient();
        var token = await TokenAsync(client, code);
        await Verify(client, token, "wrong", "203.0.113.1");
        await Verify(client, token, "wrong", "203.0.113.2");
        (await Verify(client, token, Password, "203.0.113.3")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        await Task.Delay(TimeSpan.FromSeconds(3));

        var after = await Verify(client, token, Password, "203.0.113.3");
        after.StatusCode.Should().Be(HttpStatusCode.Redirect);
        after.Headers.Location!.ToString().Should().Be(original);
    }

    [Fact]
    public async Task ForgedOrInvalidTokens_NeverReachTheLimiter_SoTheyCannotSpendOrCreateBudgets()
    {
        var (code, original) = await ProtectedLinkAsync();
        var client = Host(perLink: 2).CreateApiClient();
        var token = await TokenAsync(client, code);

        for (var i = 0; i < 10; i++)
            (await Verify(client, $"not-a-valid-token-{i}", "wrong", $"203.0.113.{i}")).StatusCode
                .Should().NotBe(HttpStatusCode.TooManyRequests);

        var legitimate = await Verify(client, token, Password, "203.0.113.99");
        legitimate.StatusCode.Should().Be(HttpStatusCode.Redirect);
        legitimate.Headers.Location!.ToString().Should().Be(original);
    }

    [Fact]
    public void DefaultCeiling_IsFarAboveWhatASingleClientCanReach_SoOneClientCannotLockALinkAlone()
    {
        var perLink = new PasswordVerificationPerLinkRateLimitOptions();
        var perClient = new PasswordVerificationRateLimitOptions();

        var singleClientAttemptsPerLinkWindow = (double)perClient.PermitLimit * perLink.WindowSeconds / perClient.WindowSeconds;

        perLink.PermitLimit.Should().BeGreaterThan((int)(singleClientAttemptsPerLinkWindow * 4));
    }
}
