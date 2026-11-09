using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;

namespace Shortly.Api.FunctionalTests.Users;

[Collection(ApiCollection.Name)]
public class ProfileTests(ApiFixture api) : ApiTestBase(api)
{
    // ---- GetLoggedInUser -----------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetLoggedInUser_ReturnsTheCallersOwnSummary()
    {
        var userId = await Seed.CreateUserAsync();
        var email = await Seed.GetUserEmailAsync(userId);
 
        var response = await As(userId).GetAsync("/api/me");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await ReadJsonAsync(response);
        body.GetProperty("id").GetGuid().Should().Be(userId);
        body.GetProperty("email").GetString().Should().Be(email);
    }
 
    [Fact]
    public async Task GetLoggedInUser_Anonymous_Returns401()
    {
        var response = await Anonymous().GetAsync("/api/me");
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    // ---- GetMyProfile / UpdateMyProfile ---------------------------------------------------------------------------
 
    [Fact]
    public async Task UpdateMyProfile_ThenGetMyProfile_ReflectsTheChange()
    {
        var userId = await Seed.CreateUserAsync();
        var client = As(userId);
 
        var update = await client.PutAsJsonAsync("/api/me/profile", new
        {
            name = "Ada Lovelace", bio = "Mathematician", phoneNumber = (string?)null, website = "https://ada.dev",
            company = "Analytical Engines Ltd", location = "London", country = "UK", timeZone = "Europe/London"
        });
 
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());
        var profile = await ReadJsonAsync(await client.GetAsync("/api/me/profile"));
        profile.GetProperty("name").GetString().Should().Be("Ada Lovelace");
        profile.GetProperty("bio").GetString().Should().Be("Mathematician");
        profile.GetProperty("company").GetString().Should().Be("Analytical Engines Ltd");
    }
 
    [Fact]
    public async Task UpdateMyProfile_WithNullFields_LeavesThemUnchanged_ButBlankFieldsClearThem()
    {
        var userId = await Seed.CreateUserAsync();
        var client = As(userId);
        await client.PutAsJsonAsync("/api/me/profile", new
        {
            name = "Original Name", bio = "Original Bio", phoneNumber = (string?)null, website = (string?)null,
            company = (string?)null, location = (string?)null, country = (string?)null, timeZone = (string?)null
        });
 
        // null = keep, blank = clear (same patch semantics as UpdateProfile documents elsewhere in the API)
        var update = await client.PutAsJsonAsync("/api/me/profile", new
        {
            name = (string?)null, bio = "", phoneNumber = (string?)null, website = (string?)null,
            company = (string?)null, location = (string?)null, country = (string?)null, timeZone = (string?)null
        });
 
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());
        var profile = await ReadJsonAsync(await client.GetAsync("/api/me/profile"));
        profile.GetProperty("name").GetString().Should().Be("Original Name");
        profile.GetProperty("bio").GetString().Should().BeNullOrEmpty();
    }
 
    [Fact]
    public async Task GetMyProfile_Anonymous_Returns401()
    {
        var response = await Anonymous().GetAsync("/api/me/profile");
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    // ---- GetMyQuotaStatus ------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetMyQuotaStatus_ReflectsTheCallersUsage()
    {
        var userId = await Seed.CreateUserAsync();
        await Seed.SetUserMonthlyLinksAsync(userId, 3);
 
        var response = await As(userId).GetAsync("/api/me/usage");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await ReadJsonAsync(response);
        body.GetProperty("monthlyLinksCreated").GetInt32().Should().Be(3);
        body.GetProperty("monthlyLinksLimit").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("remainingLinks").GetInt32().Should().Be(body.GetProperty("monthlyLinksLimit").GetInt32() - 3);
    }
 
    [Fact]
    public async Task GetMyQuotaStatus_Anonymous_Returns401()
    {
        var response = await Anonymous().GetAsync("/api/me/usage");
 
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
 
    // ---- GetMyManagedTeams -----------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetMyManagedTeams_ListsOnlyTeamsTheCallerManages()
    {
        var org = await CreateOrganizationAsync();
        var (managerId, managerMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var (otherId, _) = await org.AddMemberAsync(MemberRole.Member);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
 
        var managed = await ReadJsonAsync(await As(managerId).GetAsync("/api/me/managed-teams"));
        var notManaged = await ReadJsonAsync(await As(otherId).GetAsync("/api/me/managed-teams"));
 
        managed.EnumerateArray().Select(t => t.GetProperty("id").GetGuid()).Should().Contain(teamId);
        notManaged.EnumerateArray().Should().BeEmpty();
    }
 
    [Fact]
    public async Task GetMyManagedTeams_ForAUserWithNoTeams_ReturnsAnEmptyList()
    {
        var userId = await Seed.CreateUserAsync();
 
        var response = await As(userId).GetAsync("/api/me/managed-teams");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadJsonAsync(response)).EnumerateArray().Should().BeEmpty();
    }
}