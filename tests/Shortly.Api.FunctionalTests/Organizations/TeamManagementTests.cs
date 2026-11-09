using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;

namespace Shortly.Api.FunctionalTests.Organizations;

[Collection(ApiCollection.Name)]
public class TeamManagementTests(ApiFixture api) : ApiTestBase(api)
{
    private async Task<(OrganizationFixture Org, HttpClient Admin)> OrgWithAdminAsync()
    {
        var org = await CreateOrganizationAsync();
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        return (org, org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin));
    }
 
    // ---- list teams ------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetOrganizationTeams_ListsEveryTeamInTheOrganization()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerA) = await org.AddMemberAsync(MemberRole.TeamManager);
        var (_, managerB) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamA = await Seed.CreateTeamAsync(org.OrganizationId, managerA);
        var teamB = await Seed.CreateTeamAsync(org.OrganizationId, managerB);
 
        var response = await admin.GetAsync($"/api/organizations/{org.OrganizationId}/teams");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var ids = (await ReadJsonAsync(response)).GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("id").GetGuid());
        ids.Should().Contain([teamA, teamB]);
    }
 
    [Fact]
    public async Task GetOrganizationTeams_IsForbiddenForNonMembers()
    {
        var org = await CreateOrganizationAsync();
 
        var response = await As(Guid.NewGuid()).GetAsync($"/api/organizations/{org.OrganizationId}/teams");
 
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
 
    // ---- team members ------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task GetTeamMembers_ListsEveryMemberAddedToTheTeam()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
        var (_, memberA) = await org.AddMemberAsync(MemberRole.Member);
        await admin.PostAsJsonAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}/members", new { memberId = memberA });
 
        var response = await admin.GetAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}/members");
 
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadJsonAsync(response)).GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("memberId").GetGuid()).Should().Contain(memberA);
    }
 
    // ---- update team -------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task UpdateTeam_ChangesTheNameAndDescription()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
        var newName = $"Renamed-{Guid.NewGuid():N}";
 
        var response = await admin.PatchAsJsonAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}",
            new { name = newName, description = "A new description" });
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var team = await ReadJsonAsync(await admin.GetAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}"));
        team.GetProperty("name").GetString().Should().Be(newName);
        team.GetProperty("description").GetString().Should().Be("A new description");
    }
 
    [Fact]
    public async Task UpdateTeam_ToANameAlreadyTakenInTheOrganization_Returns409()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerA) = await org.AddMemberAsync(MemberRole.TeamManager);
        var (_, managerB) = await org.AddMemberAsync(MemberRole.TeamManager);
        var takenName = $"Taken-{Guid.NewGuid():N}";
        await Seed.CreateTeamAsync(org.OrganizationId, managerA); // first team, name does not matter here
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerB);
        await admin.PatchAsJsonAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}", new { name = takenName });
        var thirdTeam = await Seed.CreateTeamAsync(org.OrganizationId, managerA);
 
        var response = await admin.PatchAsJsonAsync($"/api/organizations/{org.OrganizationId}/teams/{thirdTeam}",
            new { name = takenName });
 
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
 
    // ---- delete team -------------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task DeleteTeam_RemovesIt_AndASubsequentGetReturns404()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
 
        var response = await admin.DeleteAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}");
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await admin.GetAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    [Fact]
    public async Task DeleteTeam_RequiresOrgAdmin_AMemberCannotDeleteIt()
    {
        var org = await CreateOrganizationAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
        var (memberUser, memberId) = await org.AddMemberAsync(MemberRole.Member);
 
        var response = await org.AsMember(memberUser, memberId, MemberRole.Member)
            .DeleteAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}");
 
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
 
    // ---- remove team member -------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task RemoveTeamMember_RemovesThem_ButNotTheManager()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
        var (_, memberId) = await org.AddMemberAsync(MemberRole.Member);
        await admin.PostAsJsonAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}/members", new { memberId });
 
        var removed = await admin.DeleteAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}/members/{memberId}");
        var removeManager = await admin.DeleteAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}/members/{managerMemberId}");
 
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent, await removed.Content.ReadAsStringAsync());
        (await Seed.IsTeamMemberAsync(teamId, memberId)).Should().BeFalse();
        removeManager.IsSuccessStatusCode.Should().BeFalse(); // the manager cannot be removed as an ordinary member
        (await Seed.GetTeamManagerAsync(teamId)).Should().Be(managerMemberId);
    }
 
    [Fact]
    public async Task RemoveTeamMember_ForSomeoneNeverAdded_Returns404()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
        var (_, neverAddedMemberId) = await org.AddMemberAsync(MemberRole.Member);
 
        var response = await admin.DeleteAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}/members/{neverAddedMemberId}");
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    // ---- update organization (PUT) ------------------------------------------------------------------------------------
 
    [Fact]
    public async Task UpdateOrganization_ByItsOwner_ChangesItsDetails()
    {
        var org = await CreateOrganizationAsync();
        var newName = $"Renamed-{Guid.NewGuid():N}";
 
        var response = await As(org.OwnerId).PutAsJsonAsync($"/api/organizations/{org.OrganizationId}", new
        {
            name = newName, description = "Updated description", website = "https://example.com", logoUrl = (string?)null
        });
 
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var mine = await ReadJsonAsync(await As(org.OwnerId).GetAsync("/api/organizations/mine"));
        mine.EnumerateArray().Single(o => o.GetProperty("organizationId").GetGuid() == org.OrganizationId)
            .GetProperty("name").GetString().Should().Be(newName);
    }
 
    [Fact]
    public async Task UpdateOrganization_ByAnOrgAdmin_WhoIsNotTheOwner_Returns403()
    {
        var (org, admin) = await OrgWithAdminAsync();
 
        var response = await admin.PutAsJsonAsync($"/api/organizations/{org.OrganizationId}", new
        {
            name = "Should not apply", description = (string?)null, website = (string?)null, logoUrl = (string?)null
        });
 
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
 
    [Fact]
    public async Task UpdateOrganization_ForAnUnknownOrganization_Returns404()
    {
        var owner = await Seed.CreateUserAsync();
 
        var response = await As(owner).PutAsJsonAsync($"/api/organizations/{Guid.NewGuid()}", new
        {
            name = "Anything", description = (string?)null, website = (string?)null, logoUrl = (string?)null
        });
 
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}