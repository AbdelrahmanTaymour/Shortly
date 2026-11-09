using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests.Auth;

[Collection(ApiCollection.Name)]
public class LoginAndRefreshTests(ApiFixture api) : ApiTestBase(api)
{
    private const string Password = "Str0ng-password!";
 
    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });
 
    private static string? RefreshCookieOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Select(v => v.Split(';')[0]).FirstOrDefault(v => v.StartsWith("refreshToken=", StringComparison.Ordinal))
            : null;
 
    private static string ErrorCode(JsonElement problem) => problem.GetProperty("errorCode").GetString()!;
 
    /// <summary>A client that sends the given refresh cookie, like a browser holding an older session.</summary>
    private HttpClient ClientWithCookie(string cookie)
    {
        var client = Api.Default.CreateApiClient();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }
 
    // ---- login -------------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokens_AndSetsTheRefreshCookie()
    {
        var (_, email) = await Seed.CreateUserWithPasswordAsync(Password);
 
        var response = await Login(Anonymous(), email, Password);
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadJsonAsync(response)).GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        RefreshCookieOf(response).Should().NotBeNull();
    }
 
    [Fact]
    public async Task Login_WithAWrongPassword_Returns401_AndCountsTheFailure()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
 
        var response = await Login(Anonymous(), email, "wrong-password");
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ErrorCode(await ReadJsonAsync(response)).Should().Be("User.InvalidCredentials");
        (await Seed.GetLoginSecurityAsync(userId)).FailedAttempts.Should().Be(1);
    }
 
    [Fact]
    public async Task Login_WithAnUnknownEmail_ReturnsTheSameErrorAsAWrongPassword()
    {
        var response = await Login(Anonymous(), "nobody@example.com", Password);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ErrorCode(await ReadJsonAsync(response)).Should().Be("User.InvalidCredentials");
    }
 
    [Fact]
    public async Task Login_LocksTheAccount_AfterFiveFailures_EvenForTheCorrectPassword()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var client = Anonymous();
        for (var i = 0; i < 5; i++) await Login(client, email, "wrong-password");
 
        var response = await Login(client, email, Password);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ErrorCode(await ReadJsonAsync(response)).Should().Be("User.AccountLocked");
        (await Seed.GetLoginSecurityAsync(userId)).LockedUntil.Should().NotBeNull();
    }
 
    [Fact]
    public async Task Login_FailuresDuringALockout_AreIgnored()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var client = Anonymous();
        for (var i = 0; i < 5; i++) await Login(client, email, "wrong-password");
        var locked = await Seed.GetLoginSecurityAsync(userId);
 
        await Login(client, email, "still-wrong");
        await Login(client, email, "still-wrong");
 
        var after = await Seed.GetLoginSecurityAsync(userId);
        after.FailedAttempts.Should().Be(5);
        after.LockedUntil.Should().Be(locked.LockedUntil);
    }
 
    [Fact]
    public async Task Login_ForADeactivatedUser_ReportsTheAccountAsDisabled_NotLocked()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        await Seed.DeactivateUserAsync(userId);
 
        var response = await Login(Anonymous(), email, Password);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ErrorCode(await ReadJsonAsync(response)).Should().Be("User.AccountDisabled");
    }
 
    // ---- refresh -----------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Refresh_WithTheLoginCookie_IssuesNewTokens_AndRotatesTheCookie()
    {
        var (_, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var login = await Login(Anonymous(), email, Password);
        var original = RefreshCookieOf(login)!;
 
        var refresh = await ClientWithCookie(original).PostAsync("/api/auth/refresh-token", null);
 
        refresh.StatusCode.Should().Be(HttpStatusCode.OK, await refresh.Content.ReadAsStringAsync());
        (await ReadJsonAsync(refresh)).GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        var rotated = RefreshCookieOf(refresh);
        rotated.Should().NotBeNull();
        rotated.Should().NotBe(original);
    }
 
    [Fact]
    public async Task Refresh_RejectsAReplayedCookie_AfterItWasRotated()
    {
        var (_, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var original = RefreshCookieOf(await Login(Anonymous(), email, Password))!;
        (await ClientWithCookie(original).PostAsync("/api/auth/refresh-token", null)).StatusCode.Should().Be(HttpStatusCode.OK);
 
        var replay = await ClientWithCookie(original).PostAsync("/api/auth/refresh-token", null);
 
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    [Fact]
    public async Task Refresh_WithoutACookie_Returns401()
    {
        var response = await Anonymous().PostAsync("/api/auth/refresh-token", null);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    [Fact]
    public async Task Refresh_IsRejected_ForAUserDeactivatedAfterLogin()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var cookie = RefreshCookieOf(await Login(Anonymous(), email, Password))!;
        await Seed.DeactivateUserAsync(userId);
 
        var response = await ClientWithCookie(cookie).PostAsync("/api/auth/refresh-token", null);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    // ---- logout ------------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var cookie = RefreshCookieOf(await Login(Anonymous(), email, Password))!;
        var client = As(userId);
        client.DefaultRequestHeaders.Add("Cookie", cookie);
 
        var logout = await client.PostAsync("/api/auth/logout", null);
        var refresh = await ClientWithCookie(cookie).PostAsync("/api/auth/refresh-token", null);
 
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    // ---- logout-all ----------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task LogoutAll_RevokesEverySession_NotJustTheCallingOne()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var sessionA = RefreshCookieOf(await Login(Anonymous(), email, Password))!;
        var sessionB = RefreshCookieOf(await Login(Anonymous(), email, Password))!;
        var caller = As(userId);
        caller.DefaultRequestHeaders.Add("Cookie", sessionA);
 
        var logoutAll = await caller.PostAsync("/api/auth/logout-all", null);
 
        logoutAll.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ClientWithCookie(sessionA).PostAsync("/api/auth/refresh-token", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ClientWithCookie(sessionB).PostAsync("/api/auth/refresh-token", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    [Fact]
    public async Task LogoutAll_Anonymous_Returns401()
    {
        var response = await Anonymous().PostAsync("/api/auth/logout-all", null);
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
