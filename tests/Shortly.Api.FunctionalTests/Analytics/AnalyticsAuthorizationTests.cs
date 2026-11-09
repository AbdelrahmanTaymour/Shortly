using System.Net;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Analytics;

[Collection(ApiCollection.Name)]
public class AnalyticsAuthorizationTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly string[] PerUrlEndpoints =
        ["overview", "engagement", "geography", "devices", "traffic", "timeseries?from=2026-01-01&to=2026-12-31"];

    public static IEnumerable<object[]> PerUrlEndpointData => PerUrlEndpoints.Select(e => new object[] { e });

    [Theory]
    [MemberData(nameof(PerUrlEndpointData))]
    public async Task PerUrlAnalytics_ForAnotherUsersLink_Returns404(string endpoint)
    {
        var owner = await Seed.CreateUserAsync();
        var linkId = await Seed.CreateUserLinkAsync(owner);
        var intruder = await Seed.CreateUserAsync();

        var response = await As(intruder).GetAsync($"/api/analytics/urls/{linkId}/{endpoint}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(PerUrlEndpointData))]
    public async Task PerUrlAnalytics_ForOwnLink_Returns200(string endpoint)
    {
        var owner = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(owner, Shortly.Domain.SubscriptionPlan.enSubscriptionPlan.Enterprise); // analytics are plan features
        var linkId = await Seed.CreateUserLinkAsync(owner);

        var response = await As(owner).GetAsync($"/api/analytics/urls/{linkId}/{endpoint}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PerUrlAnalytics_ForUnknownLink_Returns404()
    {
        var user = await Seed.CreateUserAsync();

        (await As(user).GetAsync("/api/analytics/urls/999999999/overview")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PerUrlAnalytics_BySystemAdmin_Returns200()
    {
        var owner = await Seed.CreateUserAsync();
        var linkId = await Seed.CreateUserLinkAsync(owner);

        var response = await As(Guid.NewGuid(), UserRole.SystemAdmin).GetAsync($"/api/analytics/urls/{linkId}/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PerUrlAnalytics_ForOrganizationLink_AllowedForActiveMembersOnly()
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, Shortly.Domain.SubscriptionPlan.enSubscriptionPlan.Enterprise);
        var ownerMemberId = await Seed.GetMemberIdAsync(org.OrganizationId, org.OwnerId);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, ownerMemberId);
        var (memberId, memberMemberId) = await org.AddMemberAsync(MemberRole.Viewer);
        var outsider = await Seed.CreateUserAsync();

        (await org.AsMember(memberId, memberMemberId, MemberRole.Viewer)
            .GetAsync($"/api/analytics/urls/{linkId}/overview")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await As(outsider).GetAsync($"/api/analytics/urls/{linkId}/overview")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        await Seed.RemoveMemberAsync(org.OrganizationId, memberId);
        (await org.AsMember(memberId, memberMemberId, MemberRole.Viewer)
            .GetAsync($"/api/analytics/urls/{linkId}/overview")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApproachingLimit_ReturnsOnlyTheCallersOwnLinks()
    {
        var alice = await Seed.CreateUserAsync();
        var bob = await Seed.CreateUserAsync();
        var aliceLink = await Seed.CreateUserLinkAsync(alice, clickLimit: 10);
        var bobLink = await Seed.CreateUserLinkAsync(bob, clickLimit: 10);

        var response = await As(alice).GetAsync("/api/analytics/approaching-limit?warningThreshold=0&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        var ids = json.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ToList();
        ids.Should().Contain(aliceLink);
        ids.Should().NotContain(bobLink);
        json.GetProperty("totalCount").GetInt32().Should().Be(ids.Count);
    }

    [Fact]
    public async Task MineAnalytics_InOrganizationContext_AreScopedToOrganizationLinks()
    {
        var org = await CreateOrganizationAsync();
        var ownerMemberId = await Seed.GetMemberIdAsync(org.OrganizationId, org.OwnerId);
        await Seed.CreateOrganizationLinkAsync(org.OrganizationId, ownerMemberId);
        await Seed.CreateOrganizationLinkAsync(org.OrganizationId, ownerMemberId);
        await Seed.CreateUserLinkAsync(org.OwnerId); // personal link must not be counted in organization scope

        var response = await As(org.OwnerId, organizationId: org.OrganizationId, memberId: ownerMemberId,
            memberRole: MemberRole.OrgOwner).GetAsync("/api/analytics/mine/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("totalUrls").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task MineAnalytics_InPersonalContext_AreScopedToPersonalLinks()
    {
        var user = await Seed.CreateUserAsync();
        await Seed.CreateUserLinkAsync(user);
        await Seed.CreateUserLinkAsync(user);
        await Seed.CreateUserLinkAsync(await Seed.CreateUserAsync());

        var response = await As(user).GetAsync("/api/analytics/mine/summary");

        (await ReadJsonAsync(response)).GetProperty("totalUrls").GetInt32().Should().Be(2);
    }
}
