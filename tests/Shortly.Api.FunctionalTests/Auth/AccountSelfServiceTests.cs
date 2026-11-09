using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;

namespace Shortly.Api.FunctionalTests.Auth;

[Collection(ApiCollection.Name)]
public class AccountSelfServiceTests(ApiFixture api) : ApiTestBase(api)
{
    private const string Password = "Str0ng-password!";
 
    // ---- delete account ----------------------------------------------------------------------------------------
 
    [Fact]
    public async Task DeleteAccount_IsRefused_WhileTheUserOwnsAnOrganization()
    {
        var org = await CreateOrganizationAsync();
 
        var response = await As(org.OwnerId).DeleteAsync("/api/me");
 
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("OwnsOrganizations");
    }
 
    [Fact]
    public async Task DeleteAccount_Succeeds_ForAUserWithoutOrganizations()
    {
        var user = await Seed.CreateUserAsync();
 
        (await As(user).DeleteAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
 
    // ---- verify-email -------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task VerifyEmail_WithAValidToken_ConfirmsTheEmail_AndReturnsAccessTokens()
    {
        var userId = await Seed.CreateUserAsync(); // unconfirmed by default
        var token = await Seed.CreateEmailVerificationTokenAsync(userId);
 
        var response = await Anonymous().PostAsJsonAsync("/api/account/verify-email", new { token });
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadJsonAsync(response)).GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        (await Seed.IsEmailConfirmedAsync(userId)).Should().BeTrue();
    }
 
    [Fact]
    public async Task VerifyEmail_WithAnUnknownToken_Returns404()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/account/verify-email", new { token = "not-a-real-token" });
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    [Fact]
    public async Task VerifyEmail_WithAnExpiredToken_Returns403()
    {
        var userId = await Seed.CreateUserAsync();
        var token = await Seed.CreateEmailVerificationTokenAsync(userId);
        await Seed.ExpireActionTokenAsync(token);
 
        var response = await Anonymous().PostAsJsonAsync("/api/account/verify-email", new { token });
 
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.IsEmailConfirmedAsync(userId)).Should().BeFalse();
    }
 
    [Fact]
    public async Task VerifyEmail_UsedTwice_Returns409OnTheSecondCall()
    {
        var userId = await Seed.CreateUserAsync();
        var token = await Seed.CreateEmailVerificationTokenAsync(userId);
        await Anonymous().PostAsJsonAsync("/api/account/verify-email", new { token });
 
        var replay = await Anonymous().PostAsJsonAsync("/api/account/verify-email", new { token });
 
        replay.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
 
    // ---- confirm-email-change ------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ConfirmEmailChange_WithAValidToken_ChangesTheEmail()
    {
        var userId = await Seed.CreateUserAsync();
        var newEmail = $"{Guid.NewGuid():N}@example.com";
        var token = await Seed.CreateEmailChangeTokenAsync(userId, newEmail);
 
        var response = await Anonymous().PostAsJsonAsync("/api/account/confirm-email-change", new { token });
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Seed.GetUserEmailAsync(userId)).Should().Be(newEmail);
    }
 
    [Fact]
    public async Task ConfirmEmailChange_ToAnEmailTakenInTheMeantime_Returns409_AndKeepsTheOldEmail()
    {
        var userId = await Seed.CreateUserAsync();
        var originalEmail = await Seed.GetUserEmailAsync(userId);
        var takenEmail = await Seed.GetUserEmailAsync(await Seed.CreateUserAsync());
        var token = await Seed.CreateEmailChangeTokenAsync(userId, takenEmail);
 
        var response = await Anonymous().PostAsJsonAsync("/api/account/confirm-email-change", new { token });
 
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Seed.GetUserEmailAsync(userId)).Should().Be(originalEmail);
    }
 
    [Fact]
    public async Task ConfirmEmailChange_WithAnUnknownToken_Returns404()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/account/confirm-email-change", new { token = "not-a-real-token" });
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    // ---- reset-password -------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ResetPassword_WithAValidToken_ChangesThePassword_AndRevokesExistingSessions()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var oldCookie = RefreshCookieOf(await Login(Anonymous(), email, Password))!;
        var token = await Seed.CreatePasswordResetTokenAsync(userId);
        const string newPassword = "Even-Str0nger!";
 
        var reset = await Anonymous().PostAsJsonAsync("/api/account/reset-password", new { token, newPassword });
 
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent, await reset.Content.ReadAsStringAsync());
        (await Login(Anonymous(), email, Password)).StatusCode.Should().Be(HttpStatusCode.Unauthorized); // old password no longer works
        (await Login(Anonymous(), email, newPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ClientWithCookie(oldCookie).PostAsync("/api/auth/refresh-token", null)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized); // the pre-reset session is gone too
    }
 
    [Fact]
    public async Task ResetPassword_WithAnUnknownToken_Returns404()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/account/reset-password",
            new { token = "not-a-real-token", newPassword = "Whatever-Str0ng!" });
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    [Fact]
    public async Task ResetPassword_WithATooShortPassword_Returns400_AndLeavesTheTokenUnused()
    {
        var userId = await Seed.CreateUserAsync();
        var token = await Seed.CreatePasswordResetTokenAsync(userId);
 
        var response = await Anonymous().PostAsJsonAsync("/api/account/reset-password", new { token, newPassword = "short" });
 
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // the token was never consumed, so it can still be used once a valid password is sent
        (await Anonymous().PostAsJsonAsync("/api/account/reset-password", new { token, newPassword = "Now-Str0nger!" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
 
    // ---- change-password -------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ChangePassword_WithTheCorrectCurrentPassword_Succeeds_AndRevokesOtherSessions()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
        var oldCookie = RefreshCookieOf(await Login(Anonymous(), email, Password))!;
        const string newPassword = "Even-Str0nger!";
 
        var response = await As(userId).PostAsJsonAsync("/api/account/change-password",
            new { currentPassword = Password, newPassword });
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Login(Anonymous(), email, newPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ClientWithCookie(oldCookie).PostAsync("/api/auth/refresh-token", null)).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }
 
    [Fact]
    public async Task ChangePassword_WithTheWrongCurrentPassword_Returns401_AndChangesNothing()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync(Password);
 
        var response = await As(userId).PostAsJsonAsync("/api/account/change-password",
            new { currentPassword = "totally-wrong", newPassword = "Even-Str0nger!" });
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Login(Anonymous(), email, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
 
    [Fact]
    public async Task ChangePassword_Anonymous_Returns401()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/account/change-password",
            new { currentPassword = Password, newPassword = "Even-Str0nger!" });
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    // ---- shared login/cookie helpers (mirrors LoginAndRefreshTests) -------------------------------------------------
 
    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });
 
    private static string? RefreshCookieOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.Select(v => v.Split(';')[0]).FirstOrDefault(v => v.StartsWith("refreshToken=", StringComparison.Ordinal))
            : null;
 
    private HttpClient ClientWithCookie(string cookie)
    {
        var client = Api.Default.CreateApiClient();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return client;
    }
}