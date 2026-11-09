using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Api.FunctionalTests.Organizations;

[Collection(ApiCollection.Name)]
public class ViewerReadOnlyTests(ApiFixture api) : ApiTestBase(api)
{
    private sealed record Setup(OrganizationFixture Org, HttpClient Viewer, HttpClient Member, long LinkId);

    private async Task<Setup> OrganizationAsync()
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, enSubscriptionPlan.Professional);
        var (viewerId, viewerMember) = await org.AddMemberAsync(MemberRole.Viewer);
        var (memberId, memberMember) = await org.AddMemberAsync(MemberRole.Member);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberMember);
        return new Setup(org, org.AsMember(viewerId, viewerMember, MemberRole.Viewer), org.AsMember(memberId, memberMember, MemberRole.Member), linkId);
    }

    // ---- reads stay available ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Viewer_CanStillReadOrganizationLinksAndAnalytics()
    {
        var s = await OrganizationAsync();

        (await s.Viewer.GetAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s.Viewer.GetAsync("/api/short-urls/mine")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s.Viewer.GetAsync("/api/short-urls/search?page=1&pageSize=10")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s.Viewer.GetAsync($"/api/analytics/urls/{s.LinkId}/overview")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s.Viewer.GetAsync("/api/analytics/mine/summary")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s.Viewer.GetAsync($"/api/organizations/{s.Org.OrganizationId}/members")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- writes are denied --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Viewer_CannotCreateOrganizationLinks()
    {
        var s = await OrganizationAsync();

        var single = await s.Viewer.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/viewer" });
        var bulk = await s.Viewer.PostAsJsonAsync("/api/short-urls/bulk", new { urls = new[] { new { originalUrl = "https://example.com/viewer-bulk" } } });

        single.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        bulk.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.CountOrganizationLinksAsync(s.Org.OrganizationId)).Should().Be(1);
        (await Seed.GetOrganizationMonthlyLinksAsync(s.Org.OrganizationId)).Should().Be(0);
    }

    [Fact]
    public async Task Viewer_CannotChangeOrDeleteOrganizationLinks()
    {
        var s = await OrganizationAsync();
        var link = (await Seed.GetLinkAsync(s.LinkId))!;

        (await s.Viewer.PostAsync($"/api/short-urls/{s.LinkId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await s.Viewer.PostAsJsonAsync($"/api/short-urls/{s.LinkId}/password", new { password = "viewer-password-1" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await s.Viewer.PatchAsJsonAsync($"/api/short-urls/{s.LinkId}/short-code", new { newShortCode = "viewer1234" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await s.Viewer.PutAsJsonAsync($"/api/short-urls/{s.LinkId}", new
        {
            originalUrl = "https://example.com/hijack", shortCode = link.ShortCode, title = "x", description = "x",
            clickLimit = 10, isActive = true, isPrivate = false, trackingEnabled = true, expiresAt = (DateTime?)null
        })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await s.Viewer.DeleteAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var after = (await Seed.GetLinkAsync(s.LinkId))!;
        after.IsActive.Should().BeTrue();
        after.IsPasswordProtected.Should().BeFalse();
        after.ShortCode.Should().Be(link.ShortCode);
        after.OriginalUrl.Should().Be(link.OriginalUrl);
    }

    [Fact]
    public async Task Viewer_BulkOperations_AffectNothing()
    {
        var s = await OrganizationAsync();
        var ids = new { shortUrlIds = new[] { s.LinkId } };

        var deactivate = await s.Viewer.PostAsJsonAsync("/api/short-urls/bulk/deactivate", ids);
        var delete = await s.Viewer.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/short-urls/bulk") { Content = JsonContent.Create(ids) });

        (await ReadJsonAsync(deactivate)).GetProperty("successCount").GetInt32().Should().Be(0);
        (await ReadJsonAsync(delete)).GetProperty("successCount").GetInt32().Should().Be(0);
        (await Seed.GetLinkAsync(s.LinkId))!.IsActive.Should().BeTrue();
    }

    // ---- members are unchanged -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Member_CanStillCreateAndManageOrganizationLinks()
    {
        var s = await OrganizationAsync();

        (await s.Member.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/member" })).StatusCode
            .Should().Be(HttpStatusCode.Created);
        (await s.Member.PostAsync($"/api/short-urls/{s.LinkId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await s.Member.DeleteAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task APromotedViewer_CanWriteImmediately_ARoleChangeNeedsNoNewToken()
    {
        var s = await OrganizationAsync();
        var (userId, memberId) = await s.Org.AddMemberAsync(MemberRole.Viewer);
        var client = s.Org.AsMember(userId, memberId, MemberRole.Viewer);     // the token still says Viewer
        (await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/x" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await Seed.SetMemberRoleAsync(s.Org.OrganizationId, userId, MemberRole.Member);

        // The database role decides, not the stale claim.
        (await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/x" })).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ADemotedMember_LosesWriteAccessEvenWithAMemberToken()
    {
        var s = await OrganizationAsync();
        var (userId, memberId) = await s.Org.AddMemberAsync(MemberRole.Member);
        var client = s.Org.AsMember(userId, memberId, MemberRole.Member);      // the token still says Member
        await Seed.SetMemberRoleAsync(s.Org.OrganizationId, userId, MemberRole.Viewer);

        (await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/y" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.DeleteAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
