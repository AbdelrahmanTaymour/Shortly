using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests;

/// <summary>Plan-derived limits (members, organizations), leaving an organization, and the team-name availability check.</summary>
[Collection(ApiCollection.Name)]
public class PlanAndTeamRulesTests(ApiFixture api) : ApiTestBase(api)
{
    private async Task<HttpResponseMessage> CreateOrg(Guid userId) =>
        await As(userId).PostAsJsonAsync("/api/organizations", new { name = $"Org {Guid.NewGuid():N}" });

    [Fact]
    public async Task FreeUser_CanOwnOnlyOneOrganization()
    {
        var user = await Seed.CreateUserAsync();

        (await CreateOrg(user)).StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await CreateOrg(user);

        second.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await second.Content.ReadAsStringAsync()).Should().Contain("OrganizationLimitReached");
    }

    [Fact]
    public async Task EnterpriseUser_HasNoOrganizationLimit()
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Enterprise);

        (await CreateOrg(user)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateOrg(user)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await CreateOrg(user)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangingTheOrganizationPlan_SetsTheMemberLimitFromThePlan()
    {
        var org = await CreateOrganizationAsync();
        var admin = As(Guid.NewGuid(), UserRole.SystemAdmin);

        var toStarter = await admin.PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan",
            new { subscriptionPlanId = "Starter" });
        toStarter.StatusCode.Should().Be(HttpStatusCode.NoContent, await toStarter.Content.ReadAsStringAsync());
        (await Seed.GetOrganizationMemberLimitAsync(org.OrganizationId)).Should().Be(10);

        await admin.PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan",
            new { subscriptionPlanId = "Enterprise" });
        (await Seed.GetOrganizationMemberLimitAsync(org.OrganizationId)).Should().Be(int.MaxValue);
    }

    [Fact]
    public async Task Member_CanLeave_AndOwnerCannot()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);

        var owner = await As(org.OwnerId).PostAsync($"/api/organizations/{org.OrganizationId}/members/leave", null);
        var leave = await As(userId).PostAsync($"/api/organizations/{org.OrganizationId}/members/leave", null);

        owner.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        leave.StatusCode.Should().Be(HttpStatusCode.OK, await leave.Content.ReadAsStringAsync());
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task TeamManager_CannotLeave_WhileManagingATeam()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        await Seed.CreateTeamAsync(org.OrganizationId, memberId);

        var response = await As(userId).PostAsync($"/api/organizations/{org.OrganizationId}/members/leave", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RemovingAMember_RemovesTheirTeamMemberships()
    {
        var org = await CreateOrganizationAsync();
        var (managerUser, managerId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var (targetUser, targetId) = await org.AddMemberAsync(MemberRole.Member);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerId);
        var owner = As(org.OwnerId);
        (await owner.PostAsJsonAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}/members",
            new { memberId = targetId })).StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent);

        var removed = await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/members/{targetUser}");
        var blocked = await owner.DeleteAsync($"/api/organizations/{org.OrganizationId}/members/{managerUser}");

        removed.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        (await Seed.IsTeamMemberAsync(teamId, targetId)).Should().BeFalse();
        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TeamNameAvailability_ReportsTakenNames_AndExcludesTheTeamBeingRenamed()
    {
        var org = await CreateOrganizationAsync();
        var (_, managerId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerId);
        var (viewerUser, _) = await org.AddMemberAsync(MemberRole.Viewer);
        var client = As(viewerUser);
        var url = $"/api/organizations/{org.OrganizationId}/teams/name-available";

        var free = await ReadJsonAsync(await client.GetAsync($"{url}?name=Brand-new-{Guid.NewGuid():N}"));
        var teamName = await TeamNameAsync(org, teamId);
        var taken = await ReadJsonAsync(await client.GetAsync($"{url}?name={Uri.EscapeDataString(teamName)}"));
        var own = await ReadJsonAsync(await client.GetAsync($"{url}?name={Uri.EscapeDataString(teamName)}&excludeTeamId={teamId}"));

        free.GetBoolean().Should().BeTrue();
        taken.GetBoolean().Should().BeFalse();
        own.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task TeamNameAvailability_IsForbiddenForNonMembers_AndRejectsABlankName()
    {
        var org = await CreateOrganizationAsync();
        var url = $"/api/organizations/{org.OrganizationId}/teams/name-available";

        (await As(Guid.NewGuid()).GetAsync($"{url}?name=Sales")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await As(org.OwnerId).GetAsync($"{url}?name=")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<string> TeamNameAsync(OrganizationFixture org, Guid teamId)
    {
        var team = await ReadJsonAsync(await As(org.OwnerId).GetAsync($"/api/organizations/{org.OrganizationId}/teams/{teamId}"));
        return team.GetProperty("name").GetString()!;
    }

    [Fact]
    public async Task DeletingAnOrganization_DeletesItsLinks_ButNotTheOwnersPersonalOnes()
    {
        var org = await CreateOrganizationAsync();
        var memberId = await Seed.GetMemberIdAsync(org.OrganizationId, org.OwnerId);
        var orgLink = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId);
        var personalLink = await Seed.CreateUserLinkAsync(org.OwnerId);

        var response = await As(org.OwnerId).DeleteAsync($"/api/organizations/{org.OrganizationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Seed.LinkExistsAsync(orgLink)).Should().BeFalse();
        (await Seed.LinkExistsAsync(personalLink)).Should().BeTrue();
    }
}
