using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// Tracking metadata (User-Agent, Referer, UTM parameters, session cookie) is best-effort: missing or oversized values must
/// never stop a valid redirect, and what is stored is cut to the column lengths so the click is not lost.
/// </summary>
[Collection(ApiCollection.Name)]
public class RedirectMetadataTests(ApiFixture api) : ApiTestBase(api)
{
    private async Task<(long LinkId, string ShortCode, string OriginalUrl)> TrackedLinkAsync()
    {
        var owner = await Seed.CreateUserAsync();
        var linkId = await Seed.CreateUserLinkAsync(owner);      // tracking is enabled by default
        var link = (await Seed.GetLinkAsync(linkId))!;
        return (linkId, link.ShortCode, link.OriginalUrl);
    }

    private HttpClient ClientWithoutUserAgent()
    {
        var client = Anonymous();
        client.DefaultRequestHeaders.UserAgent.Clear();
        return client;
    }

    // ---- missing metadata -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Redirect_WithoutAUserAgent_Succeeds_AndTheClickIsRecordedWithAnEmptyUserAgent()
    {
        var (linkId, code, original) = await TrackedLinkAsync();

        var response = await ClientWithoutUserAgent().GetAsync($"/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(original);
        var click = (await Seed.WaitForClicksAsync(linkId)).Single();
        click.UserAgent.Should().BeEmpty();
    }

    [Fact]
    public async Task Redirect_WithNoMetadataAtAll_Succeeds()
    {
        var (linkId, code, original) = await TrackedLinkAsync();

        // No User-Agent, no Referer, no UTM parameters, no cookies.
        var response = await ClientWithoutUserAgent().GetAsync($"/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var click = (await Seed.WaitForClicksAsync(linkId)).Single();
        click.Referrer.Should().BeNull();
        click.UtmSource.Should().BeNull();
    }

    // ---- oversized metadata ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Redirect_WithOversizedMetadata_Succeeds_AndStoresValuesCutToTheColumnLengths()
    {
        var (linkId, code, original) = await TrackedLinkAsync();
        var client = Anonymous();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", new string('u', 2000));
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://ref.example.com/" + new string('r', 2000));
        var utm = new string('x', 300);

        var response = await client.GetAsync($"/{code}?utm_source={utm}&utm_medium={utm}&utm_campaign={utm}&utm_term={utm}&utm_content={utm}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be(original);

        var click = (await Seed.WaitForClicksAsync(linkId)).Single();       // saved, not lost to a column overflow
        click.UserAgent.Should().HaveLength(500);
        click.Referrer.Should().HaveLength(500);
        click.UtmSource.Should().HaveLength(100);
        click.UtmMedium.Should().HaveLength(100);
        click.UtmCampaign.Should().HaveLength(100);
        click.UtmTerm.Should().HaveLength(100);
        click.UtmContent.Should().HaveLength(100);
    }

    [Fact]
    public async Task Redirect_StoresValuesWithinTheLimitsUnchanged()
    {
        var (linkId, code, _) = await TrackedLinkAsync();
        var client = Anonymous();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", new string('u', 500));
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://example.org/page");

        (await client.GetAsync($"/{code}?utm_source={new string('s', 100)}&utm_campaign=spring")).StatusCode
            .Should().Be(HttpStatusCode.Redirect);

        var click = (await Seed.WaitForClicksAsync(linkId)).Single();
        click.UserAgent.Should().HaveLength(500);                              // exactly at the limit: untouched
        click.UtmSource.Should().HaveLength(100);
        click.UtmCampaign.Should().Be("spring");
        click.Referrer.Should().Be("https://example.org/page");
        click.ReferrerDomain.Should().Be("example.org");
    }

    [Fact]
    public async Task Redirect_WithAVeryLongReferrerHost_StoresTheDomainCutTo100Characters()
    {
        var (linkId, code, _) = await TrackedLinkAsync();
        var host = $"{new string('a', 60)}.{new string('b', 60)}.{new string('c', 60)}.com";   // 186 characters
        var client = Anonymous();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", $"https://{host}/page");

        (await client.GetAsync($"/{code}")).StatusCode.Should().Be(HttpStatusCode.Redirect);

        var click = (await Seed.WaitForClicksAsync(linkId)).Single();
        click.ReferrerDomain.Should().HaveLength(100);
        host.Should().StartWith(click.ReferrerDomain!);
    }

    [Fact]
    public async Task Truncation_NeverSplitsASurrogatePair_SoTheClickIsStillSaved()
    {
        var (linkId, code, _) = await TrackedLinkAsync();
        var client = Anonymous();

        // HTTP headers are ASCII/Latin-1, so the surrogate case is exercised through a UTM value, which is UTF-8 in the URL.
        // 99 ASCII characters then an emoji (two UTF-16 chars): a naive cut at 100 would leave half of the pair.
        var emojiUtm = new string('a', 99) + "\U0001F600" + "tail";
        (await client.GetAsync($"/{code}?utm_source={Uri.EscapeDataString(emojiUtm)}")).StatusCode.Should().Be(HttpStatusCode.Redirect);

        var click = (await Seed.WaitForClicksAsync(linkId)).Single();
        click.UtmSource.Should().Be(new string('a', 99));                      // cut before the pair, not through it
    }

    [Fact]
    public async Task Redirect_WithAnOversizedSessionCookie_Succeeds_AndAFreshSessionIdIsUsed()
    {
        var (linkId, code, _) = await TrackedLinkAsync();
        var client = Api.Default.CreateApiClient();
        client.DefaultRequestHeaders.Add("Cookie", $"shortly_sid={new string('s', 300)}");

        var response = await client.GetAsync($"/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var click = (await Seed.WaitForClicksAsync(linkId)).Single();
        click.SessionId.Length.Should().BeInRange(1, 128);
        click.SessionId.Should().NotBe(new string('s', 300));
    }

    [Fact]
    public async Task Redirect_WithAValidSessionCookie_KeepsIt()
    {
        var (linkId, code, _) = await TrackedLinkAsync();
        var client = Api.Default.CreateApiClient();
        client.DefaultRequestHeaders.Add("Cookie", "shortly_sid=my-existing-session");

        (await client.GetAsync($"/{code}")).StatusCode.Should().Be(HttpStatusCode.Redirect);

        (await Seed.WaitForClicksAsync(linkId)).Single().SessionId.Should().Be("my-existing-session");
    }

    // ---- existing behavior is preserved ----------------------------------------------------------------------------------

    [Fact]
    public async Task Redirect_ForALinkWithTrackingDisabled_StillRedirectsAndRecordsNothing()
    {
        var anonymousLinkId = await Seed.CreateAnonymousLinkAsync();      // anonymous links have tracking disabled
        var link = (await Seed.GetLinkAsync(anonymousLinkId))!;

        var response = await ClientWithoutUserAgent().GetAsync($"/{link.ShortCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        await Task.Delay(500);
        (await Seed.CountClickEventsAsync(anonymousLinkId)).Should().Be(0);
    }

    [Fact]
    public async Task Redirect_ForAnUnknownCode_Returns404()
    {
        (await Anonymous().GetAsync($"/nope{Guid.NewGuid():N}"[..20])).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---- password-protected links --------------------------------------------------------------------------------------------

    [Fact]
    public async Task PasswordFlow_WorksWithoutAUserAgent_AndWithOversizedMetadata()
    {
        var owner = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(owner, enSubscriptionPlan.Professional);
        var linkId = await Seed.CreateUserLinkAsync(owner);
        (await As(owner).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = "metadata-test-pw-1" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var link = (await Seed.GetLinkAsync(linkId))!;
        var client = Api.CreateFactory().CreateApiClient();           // a host whose limiters are untouched by other tests
        client.DefaultRequestHeaders.UserAgent.Clear();

        var page = await client.GetAsync($"/{link.ShortCode}");
        page.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var token = System.Web.HttpUtility.ParseQueryString(new Uri(new Uri("https://localhost"), page.Headers.Location!).Query)["token"]!;

        async Task<HttpResponseMessage> Verify(string password)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/url-redirect/verify")
            {
                Content = new FormUrlEncodedContent([new("token", token), new("password", password)])
            };
            request.Headers.TryAddWithoutValidation("Referer", "https://ref.example.com/" + new string('r', 2000));
            return await client.SendAsync(request);
        }

        (await Verify("wrong")).StatusCode.Should().Be(HttpStatusCode.OK);                 // page re-rendered with an error
        var ok = await Verify("metadata-test-pw-1");
        ok.StatusCode.Should().Be(HttpStatusCode.Redirect);
        ok.Headers.Location!.ToString().Should().Be(link.OriginalUrl);
    }
}
