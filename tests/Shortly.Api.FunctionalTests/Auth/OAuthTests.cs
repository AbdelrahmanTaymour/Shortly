using System.Net;
using System.Web;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests.Auth;

[Collection(ApiCollection.Name)]
public class OAuthTests(ApiFixture api) : ApiTestBase(api)
{
    // ---- google/login --------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GoogleLogin_Challenges_AndPointsBackAtOurOwnCallback()
    {
        var response = await Anonymous().GetAsync("/api/oauth/google/login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var location = response.Headers.Location!;
        location.Host.Should().Be("accounts.google.com");

        var query = HttpUtility.ParseQueryString(location.Query);

        query["client_id"].Should().Be("test-client-id");
        query["response_type"].Should().Be("code");
        query["redirect_uri"].Should().Be("https://localhost/signin-google");
        query["state"].Should().NotBeNullOrWhiteSpace();
    }
 
    [Fact]
    public async Task GoogleLogin_ShouldRedirectToGoogleWithCorrectCallbackUri()
    {
        var response = await Anonymous().GetAsync(
            "/api/oauth/google/login?returnUrl=%2Fdashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var location = response.Headers.Location;
        location.Should().NotBeNull();

        var query = HttpUtility.ParseQueryString(location!.Query);

        query["redirect_uri"].Should().Be("https://localhost/signin-google");
    }
 
    // ---- google/callback -------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GoogleCallback_WithoutACompletedGoogleHandshake_RedirectsToLogin_WithAnError()
    {
        var response = await Anonymous().GetAsync("/api/oauth/google/callback");
 
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/login");
        location.Should().Contain("error=");
    }
 
    [Fact]
    public async Task GoogleCallback_IsAnonymous_AndDoesNotRequireAnAccessToken()
    {
        var response = await Anonymous().GetAsync("/api/oauth/google/callback");
 
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }
}