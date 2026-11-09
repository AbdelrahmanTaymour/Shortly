using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Admin;

[Collection(ApiCollection.Name)]
public class AdminUserManagementTests(ApiFixture api) : ApiTestBase(api)
{
    private async Task<HttpClient> SystemAdminAsync() => As(await Seed.CreateUserAsync(), UserRole.SystemAdmin);
    
    private async Task<JsonElement> GetByIdAsync(HttpClient admin, Guid userId)
    {
        var response = await admin.GetAsync($"/api/admin/users/{userId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await ReadJsonAsync(response);
    }
 
    // ---- search --------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Search_FiltersByStatus_AndIsAdminOnly()
    {
        var active = await Seed.CreateUserAsync();
        var activeEmail = await Seed.GetUserEmailAsync(active);
        var inactive = await Seed.CreateUserAsync();
        var inactiveEmail = await Seed.GetUserEmailAsync(inactive);
        await Seed.DeactivateUserAsync(inactive);
        var admin = await SystemAdminAsync();
        var activeQuery = Uri.EscapeDataString(activeEmail);
        var inactiveQuery = Uri.EscapeDataString(inactiveEmail);
 
        var activeMatch = await ReadJsonAsync(await admin.GetAsync($"/api/admin/users/search?status=Active&search={activeQuery}"));
        var activeMatchAsInactive = await ReadJsonAsync(await admin.GetAsync($"/api/admin/users/search?status=Inactive&search={activeQuery}"));
        var inactiveMatch = await ReadJsonAsync(await admin.GetAsync($"/api/admin/users/search?status=Inactive&search={inactiveQuery}"));
        var inactiveMatchAsActive = await ReadJsonAsync(await admin.GetAsync($"/api/admin/users/search?status=Active&search={inactiveQuery}"));
        var forbidden = await As(await Seed.CreateUserAsync()).GetAsync("/api/admin/users/search");
 
        AllIds(activeMatch).Should().Contain(active);
        AllIds(activeMatchAsInactive).Should().NotContain(active);
        AllIds(inactiveMatch).Should().Contain(inactive);
        AllIds(inactiveMatchAsActive).Should().NotContain(inactive);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
 
    private static IEnumerable<Guid> AllIds(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("id").GetGuid());
 
    // ---- lookups ---------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetByEmailAndByUsername_ReturnTheSameUser_AsGetById()
    {
        var userId = await Seed.CreateUserAsync();
        var email = await Seed.GetUserEmailAsync(userId);
        var admin = await SystemAdminAsync();
 
        var byId = await GetByIdAsync(admin, userId);
        var byEmail = await ReadJsonAsync(await admin.GetAsync($"/api/admin/users/by-email?email={Uri.EscapeDataString(email)}"));
        var byUsername = await ReadJsonAsync(await admin.GetAsync(
            $"/api/admin/users/by-username?username={Uri.EscapeDataString(byId.GetProperty("username").GetString()!)}"));
 
        byEmail.GetProperty("id").GetGuid().Should().Be(userId);
        byUsername.GetProperty("id").GetGuid().Should().Be(userId);
    }
 
    [Fact]
    public async Task GetById_ForAnUnknownUser_Returns404()
    {
        var response = await (await SystemAdminAsync()).GetAsync($"/api/admin/users/{Guid.NewGuid()}");
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    // ---- create ----------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Create_ReturnsCreatedAtGetById_AndTheUserCanThenLogIn()
    {
        var admin = await SystemAdminAsync();
        var email = $"{Guid.NewGuid():N}@example.com";
        var request = new { email, username = $"u{Guid.NewGuid():N}"[..20], password = "Str0ng-password!", role = UserRole.User };
 
        var response = await admin.PostAsJsonAsync("/api/admin/users", request);
 
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        response.Headers.Location.Should().NotBeNull();
        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password = "Str0ng-password!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }
 
    [Fact]
    public async Task Create_WithAnEmailAlreadyInUse_Returns409()
    {
        var existingEmail = await Seed.GetUserEmailAsync(await Seed.CreateUserAsync());
 
        var response = await (await SystemAdminAsync()).PostAsJsonAsync("/api/admin/users", new
        {
            email = existingEmail, username = $"u{Guid.NewGuid():N}"[..20], password = "Str0ng-password!", role = UserRole.User
        });
 
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
 
    [Fact]
    public async Task Create_IsAdminOnly()
    {
        var response = await As(await Seed.CreateUserAsync()).PostAsJsonAsync("/api/admin/users", new
        {
            email = $"{Guid.NewGuid():N}@example.com", username = "someone", password = "Str0ng-password!", role = UserRole.User
        });
 
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
 
    // ---- force-update ------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ForceUpdate_AppliesOnlyTheSuppliedFields_AndLeavesTheRestUnchanged()
    {
        var userId = await Seed.CreateUserAsync();
        var admin = await SystemAdminAsync();
        var before = await GetByIdAsync(admin, userId);
 
        var response = await admin.PatchAsJsonAsync($"/api/admin/users/{userId}", new { role = UserRole.SuperAdmin });
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var after = await GetByIdAsync(admin, userId);
        after.GetProperty("role").GetString().Should().Be(nameof(UserRole.SuperAdmin));
        after.GetProperty("email").GetString().Should().Be(before.GetProperty("email").GetString());
        after.GetProperty("username").GetString().Should().Be(before.GetProperty("username").GetString());
    }
 
    [Fact]
    public async Task ForceUpdate_CanDeactivateAUser_DirectlyWithoutTheBulkEndpoint()
    {
        var userId = await Seed.CreateUserAsync();
        var admin = await SystemAdminAsync();
 
        var response = await admin.PatchAsJsonAsync($"/api/admin/users/{userId}", new { isActive = false });
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetByIdAsync(admin, userId)).GetProperty("isActive").GetBoolean().Should().BeFalse();
    }
 
    [Fact]
    public async Task ForceUpdate_ForAnUnknownUser_Returns404()
    {
        var response = await (await SystemAdminAsync()).PatchAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}", new { role = UserRole.User });
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    // ---- unlock ------------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task Unlock_ClearsTheLockout_AndLetsTheUserLogInAgain()
    {
        var (userId, email) = await Seed.CreateUserWithPasswordAsync("Str0ng-password!");
        var anon = Anonymous();
        for (var i = 0; i < 5; i++)
            await anon.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });
        (await Seed.GetLoginSecurityAsync(userId)).LockedUntil.Should().NotBeNull();
 
        var unlock = await (await SystemAdminAsync()).PostAsync($"/api/admin/users/{userId}/unlock", null);
 
        unlock.StatusCode.Should().Be(HttpStatusCode.NoContent, await unlock.Content.ReadAsStringAsync());
        (await Seed.GetLoginSecurityAsync(userId)).LockedUntil.Should().BeNull();
        (await Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password = "Str0ng-password!" })).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }
 
    [Fact]
    public async Task Unlock_ForAnUnknownUser_Returns404()
    {
        var response = await (await SystemAdminAsync()).PostAsync($"/api/admin/users/{Guid.NewGuid()}/unlock", null);
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    // ---- bulk activate / deactivate / delete --------------------------------------------------------------------------
 
    [Fact]
    public async Task BulkActivate_ReactivatesEveryListedUser_AndReportsTheCount()
    {
        var a = await Seed.CreateUserAsync();
        var b = await Seed.CreateUserAsync();
        await Seed.DeactivateUserAsync(a);
        await Seed.DeactivateUserAsync(b);
        var admin = await SystemAdminAsync();
 
        var response = await admin.PostAsJsonAsync("/api/admin/users/bulk/activate", new { userIds = new[] { a, b } });
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await ReadJsonAsync(response);
        body.GetProperty("successCount").GetInt32().Should().Be(2);
        (await GetByIdAsync(admin, a)).GetProperty("isActive").GetBoolean().Should().BeTrue();
        (await GetByIdAsync(admin, b)).GetProperty("isActive").GetBoolean().Should().BeTrue();
    }
 
    [Fact]
    public async Task BulkDeactivate_DisablesEveryListedUser()
    {
        var a = await Seed.CreateUserAsync();
        var b = await Seed.CreateUserAsync();
        var admin = await SystemAdminAsync();
 
        var response = await admin.PostAsJsonAsync("/api/admin/users/bulk/deactivate", new { userIds = new[] { a, b } });
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await GetByIdAsync(admin, a)).GetProperty("isActive").GetBoolean().Should().BeFalse();
        (await GetByIdAsync(admin, b)).GetProperty("isActive").GetBoolean().Should().BeFalse();
    }
 
    [Fact]
    public async Task BulkDelete_ReportsPartialSuccess_WhenOneIdIsUnknown()
    {
        var admin = await SystemAdminAsync();
        var known = await Seed.CreateUserAsync();
        var unknown = Guid.NewGuid();
 
        var response = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/admin/users/bulk")
        {
            Content = JsonContent.Create(new { userIds = new[] { known, unknown } })
        });
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await ReadJsonAsync(response);
        body.GetProperty("totalProcessed").GetInt32().Should().Be(2);
        body.GetProperty("successCount").GetInt32().Should().Be(1);
        body.GetProperty("failureCount").GetInt32().Should().Be(1);
        (await admin.GetAsync($"/api/admin/users/{known}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
 
    [Fact]
    public async Task BulkOperations_AreAdminOnly()
    {
        var caller = As(await Seed.CreateUserAsync());
 
        (await caller.PostAsJsonAsync("/api/admin/users/bulk/activate", new { userIds = Array.Empty<Guid>() })).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await caller.PostAsJsonAsync("/api/admin/users/bulk/deactivate", new { userIds = Array.Empty<Guid>() })).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }
 
    // ---- usage resets -----------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task ResetUserUsage_ZeroesTheMonthlyCount_ButKeepsTheTotal()
    {
        var userId = await Seed.CreateUserAsync();
        await Seed.SetUserMonthlyLinksAsync(userId, 7);
        var admin = await SystemAdminAsync();
        var before = await GetByIdAsync(admin, userId);
 
        var response = await admin.PostAsync($"/api/admin/users/{userId}/usage/reset", null);
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var after = await GetByIdAsync(admin, userId);
        after.GetProperty("monthlyLinksCreated").GetInt32().Should().Be(0);
        after.GetProperty("totalLinksCreated").GetInt32().Should().Be(before.GetProperty("totalLinksCreated").GetInt32());
    }
 
    [Fact]
    public async Task ResetAllUsersUsage_ReportsHowManyUsersWereAffected()
    {
        var a = await Seed.CreateUserAsync();
        var b = await Seed.CreateUserAsync();
        await Seed.SetUserMonthlyLinksAsync(a, 3);
        await Seed.SetUserMonthlyLinksAsync(b, 4);
        var admin = await SystemAdminAsync();
 
        var response = await admin.PostAsync("/api/admin/users/usage/reset-all", null);
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadJsonAsync(response)).GetProperty("usersAffected").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        (await GetByIdAsync(admin, a)).GetProperty("monthlyLinksCreated").GetInt32().Should().Be(0);
        (await GetByIdAsync(admin, b)).GetProperty("monthlyLinksCreated").GetInt32().Should().Be(0);
    }
 
    [Fact]
    public async Task UsageResets_AreAdminOnly()
    {
        var caller = As(await Seed.CreateUserAsync());
 
        (await caller.PostAsync($"/api/admin/users/{Guid.NewGuid()}/usage/reset", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await caller.PostAsync("/api/admin/users/usage/reset-all", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}