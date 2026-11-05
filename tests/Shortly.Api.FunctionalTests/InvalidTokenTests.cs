using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// Phase 0.5: POST /api/short-urls is [AllowAnonymous] but behaves differently for authenticated callers.
/// An invalid token must be rejected, never silently downgraded to an anonymous request.
/// </summary>
[Collection(ApiCollection.Name)]
public class InvalidTokenTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly object Body = new { originalUrl = "https://example.com/token-test" };

    private async Task<HttpResponseMessage> CreateWith(string? authorizationHeader)
    {
        var client = Anonymous();
        if (authorizationHeader is not null)
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationHeader);
        return await client.PostAsJsonAsync("/api/short-urls", Body);
    }

    [Fact]
    public async Task Create_WithoutToken_IsAllowedAsAnonymous()
    {
        var response = await CreateWith(null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_WithValidToken_Succeeds()
    {
        var userId = await Seed.CreateUserAsync();

        var response = await As(userId).PostAsJsonAsync("/api/short-urls", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_WithExpiredToken_Returns401_NotAnonymousLink()
    {
        var token = TestTokens.Create(await Seed.CreateUserAsync(), expired: true);

        var response = await CreateWith($"Bearer {token}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().Should().Contain("invalid_token");
    }

    [Fact]
    public async Task Create_WithMalformedToken_Returns401()
    {
        (await CreateWith("Bearer this-is-not-a-jwt")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithTokenSignedByAnotherKey_Returns401()
    {
        var forged = TestTokens.Create(
            await Seed.CreateUserAsync(), signingKey: "a-completely-different-signing-key-of-sufficient-length!!");

        (await CreateWith($"Bearer {forged}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithEmptyBearerToken_Returns401()
    {
        (await CreateWith("Bearer ")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithNonBearerAuthorizationHeader_IsTreatedAsNoToken()
    {
        (await CreateWith("Basic dXNlcjpwYXNz")).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("/api/organizations/mine")]
    [InlineData("/api/short-urls/mine")]
    [InlineData("/api/analytics/mine/summary")]
    public async Task RequiredAuthEndpoints_RejectExpiredAndMalformedTokens(string url)
    {
        var expired = TestTokens.Create(await Seed.CreateUserAsync(), expired: true);

        var withExpired = Api.Default.CreateApiClient(expired);
        var withMalformed = Api.Default.CreateApiClient("garbage");

        (await withExpired.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await withMalformed.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OtherAnonymousEndpoints_AreUnaffectedByAStaleToken()
    {
        // The filter is scoped to short-url creation: e.g. the invitation-validation page may still be called
        // by a browser that holds an expired token.
        var expired = TestTokens.Create(await Seed.CreateUserAsync(), expired: true);

        var response = await Api.Default.CreateApiClient(expired).GetAsync("/api/invitations/validate?token=unknown-token");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
