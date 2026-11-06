using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// Phase 0.4: an access token keeps its organization claims until it expires (up to 15 minutes).
/// Once a member is removed, that token must no longer grant access to organization resources.
/// </summary>
[Collection(ApiCollection.Name)]
public class StaleOrganizationClaimTests(ApiFixture api) : ApiTestBase(api)
{
    private sealed record Scenario(
        OrganizationFixture Org, Guid UserId, Guid MemberId, long OrganizationLinkId, HttpClient StaleToken);

    /// <summary>A member whose token carries the organization claim and who is then removed from it.</summary>
    private async Task<Scenario> RemovedMemberAsync(MemberRole role = MemberRole.Member)
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(role);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId);
        var token = org.AsMember(userId, memberId, role);

        await Seed.RemoveMemberAsync(org.OrganizationId, userId);
        return new Scenario(org, userId, memberId, linkId, token);
    }

    [Fact]
    public async Task ActiveMember_CanReadAndManageOrganizationLinks()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId);
        var client = org.AsMember(userId, memberId, MemberRole.Member);

        (await client.GetAsync($"/api/short-urls/{linkId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/short-urls/mine")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/api/short-urls/{linkId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemovedMember_CannotReadOrganizationLinkById()
    {
        var s = await RemovedMemberAsync();

        var response = await s.StaleToken.GetAsync($"/api/short-urls/{s.OrganizationLinkId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemovedMember_CannotListOrganizationLinks()
    {
        var s = await RemovedMemberAsync();

        (await s.StaleToken.GetAsync("/api/short-urls/mine")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.StaleToken.GetAsync("/api/short-urls/search?page=1&pageSize=10")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemovedMember_CannotDeleteOrganizationLink()
    {
        var s = await RemovedMemberAsync();

        var response = await s.StaleToken.DeleteAsync($"/api/short-urls/{s.OrganizationLinkId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Seed.LinkExistsAsync(s.OrganizationLinkId)).Should().BeTrue();
    }

    [Fact]
    public async Task RemovedMember_CannotModifyOrganizationLink()
    {
        var s = await RemovedMemberAsync();

        var response = await s.StaleToken.PostAsync($"/api/short-urls/{s.OrganizationLinkId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemovedMember_CannotCreateOrganizationLink()
    {
        var s = await RemovedMemberAsync();

        var response = await s.StaleToken.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/stale" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemovedMember_CannotBulkCreateOrganizationLinks()
    {
        var s = await RemovedMemberAsync();

        var response = await s.StaleToken.PostAsJsonAsync("/api/short-urls/bulk",
            new { urls = new[] { new { originalUrl = "https://example.com/stale-bulk" } } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RemovedMember_CannotReadOrganizationAnalytics()
    {
        var s = await RemovedMemberAsync();

        (await s.StaleToken.GetAsync("/api/analytics/mine/overview")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.StaleToken.GetAsync("/api/analytics/mine/summary")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.StaleToken.GetAsync($"/api/analytics/urls/{s.OrganizationLinkId}/overview")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemovedMember_CanLeaveOrganizationContextAndKeepUsingPersonalLinks()
    {
        var s = await RemovedMemberAsync();

        // POST /organizations/exit only requires authentication and re-issues a personal-scope token.
        var exit = await s.StaleToken.PostAsync("/api/organizations/exit", null);
        exit.StatusCode.Should().Be(HttpStatusCode.OK, await exit.Content.ReadAsStringAsync());

        var accessToken = (await ReadJsonAsync(exit)).GetProperty("accessToken").GetString()!;
        var personal = Api.Default.CreateApiClient(accessToken);

        (await personal.GetAsync("/api/short-urls/mine")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await personal.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/personal" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task MemberOfOrganizationA_WithClaimForA_CannotTouchOrganizationBLinks()
    {
        var orgA = await CreateOrganizationAsync();
        var orgB = await CreateOrganizationAsync();
        var (userId, memberId) = await orgA.AddMemberAsync(MemberRole.Member);
        var linkInB = await Seed.CreateOrganizationLinkAsync(orgB.OrganizationId, await MemberIdOf(orgB));
        var client = orgA.AsMember(userId, memberId, MemberRole.Member);

        (await client.GetAsync($"/api/short-urls/{linkInB}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.DeleteAsync($"/api/short-urls/{linkInB}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Seed.LinkExistsAsync(linkInB)).Should().BeTrue();
    }

    [Fact]
    public async Task ForgedOrganizationClaim_ForOrganizationTheUserNeverJoined_IsRejected()
    {
        var org = await CreateOrganizationAsync();
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, await MemberIdOf(org));
        var attacker = await Seed.CreateUserAsync();

        // Correctly signed token, but the claimed membership does not exist in the database.
        var client = As(attacker, organizationId: org.OrganizationId, memberId: Guid.NewGuid(), memberRole: MemberRole.OrgOwner);

        (await client.GetAsync($"/api/short-urls/{linkId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.DeleteAsync($"/api/short-urls/{linkId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/forged" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.LinkExistsAsync(linkId)).Should().BeTrue();
    }

    private Task<Guid> MemberIdOf(OrganizationFixture org) =>
        Seed.GetMemberIdAsync(org.OrganizationId, org.OwnerId);
}
