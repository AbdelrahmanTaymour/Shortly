using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;

namespace Shortly.Api.FunctionalTests.ShortUrls;

[Collection(ApiCollection.Name)]
public class BulkOperationsAuthorizationTests(ApiFixture api) : ApiTestBase(api)
{
    private static HttpRequestMessage DeleteWithBody(string url, object body) =>
        new(HttpMethod.Delete, url) { Content = JsonContent.Create(body) };

    private static async Task<(int Total, int Succeeded, int Failed)> ReadCounts(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return (json.GetProperty("totalProcessed").GetInt32(), json.GetProperty("successCount").GetInt32(),
            json.GetProperty("failureCount").GetInt32());
    }

    private async Task<(Guid A, Guid B, long LinkA, long LinkB)> TwoUsersWithOneLinkEachAsync()
    {
        var a = await Seed.CreateUserAsync();
        var b = await Seed.CreateUserAsync();
        return (a, b, await Seed.CreateUserLinkAsync(a), await Seed.CreateUserLinkAsync(b));
    }

    [Fact]
    public async Task BulkDelete_OnlyDeletesCallersOwnLinks()
    {
        var (a, b, linkA, linkB) = await TwoUsersWithOneLinkEachAsync();

        var response = await As(b).SendAsync(DeleteWithBody("/api/short-urls/bulk", new { shortUrlIds = new[] { linkA, linkB } }));

        (await ReadCounts(response)).Should().Be((2, 1, 1));
        (await Seed.LinkExistsAsync(linkA)).Should().BeTrue();
        (await Seed.LinkExistsAsync(linkB)).Should().BeFalse();
    }

    [Fact]
    public async Task BulkDeactivateAndActivate_OnlyAffectCallersOwnLinks()
    {
        var (a, b, linkA, linkB) = await TwoUsersWithOneLinkEachAsync();
        var ids = new { shortUrlIds = new[] { linkA, linkB } };

        (await ReadCounts(await As(b).PostAsJsonAsync("/api/short-urls/bulk/deactivate", ids))).Should().Be((2, 1, 1));
        (await Seed.GetLinkAsync(linkA))!.IsActive.Should().BeTrue();
        (await Seed.GetLinkAsync(linkB))!.IsActive.Should().BeFalse();

        // The owner of A deactivates A; B then tries to activate both: A must stay inactive.
        await As(a).PostAsJsonAsync("/api/short-urls/bulk/deactivate", new { shortUrlIds = new[] { linkA } });
        (await ReadCounts(await As(b).PostAsJsonAsync("/api/short-urls/bulk/activate", ids))).Should().Be((2, 1, 1));
        (await Seed.GetLinkAsync(linkA))!.IsActive.Should().BeFalse();
        (await Seed.GetLinkAsync(linkB))!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task BulkExpiration_OnlyAffectsCallersOwnLinks()
    {
        var (a, b, linkA, linkB) = await TwoUsersWithOneLinkEachAsync();
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var response = await As(b).PatchAsJsonAsync("/api/short-urls/bulk/expiration",
            new { shortUrlIds = new[] { linkA, linkB }, expiresAt });

        (await ReadCounts(response)).Should().Be((2, 1, 1));
        (await Seed.GetLinkAsync(linkA))!.ExpiresAt.Should().BeNull();
        (await Seed.GetLinkAsync(linkB))!.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BulkOperations_WithUnknownIds_CountThemAsFailures()
    {
        var user = await Seed.CreateUserAsync();
        var own = await Seed.CreateUserLinkAsync(user);

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk/deactivate",
            new { shortUrlIds = new[] { own, 987654321L } });

        (await ReadCounts(response)).Should().Be((2, 1, 1));
    }

    // ---- organization links ---------------------------------------------------------------------

    private async Task<(OrganizationFixture Org, Guid MemberUser, Guid MemberId, long OrgLink)> OrgWithMemberAndLinkAsync()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        return (org, userId, memberId, await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId));
    }

    [Fact]
    public async Task BulkOperations_ByActiveMember_AffectOrganizationLinks()
    {
        var (org, userId, memberId, orgLink) = await OrgWithMemberAndLinkAsync();

        var response = await org.AsMember(userId, memberId, MemberRole.Member)
            .PostAsJsonAsync("/api/short-urls/bulk/deactivate", new { shortUrlIds = new[] { orgLink } });

        (await ReadCounts(response)).Should().Be((1, 1, 0));
        (await Seed.GetLinkAsync(orgLink))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task BulkOperations_ByMemberOfAnotherOrganization_DoNotAffectTheLinks()
    {
        var (_, _, _, orgLink) = await OrgWithMemberAndLinkAsync();
        var otherOrg = await CreateOrganizationAsync();
        var (userId, memberId) = await otherOrg.AddMemberAsync(MemberRole.OrgAdmin);
        var client = otherOrg.AsMember(userId, memberId, MemberRole.OrgAdmin);
        var ids = new { shortUrlIds = new[] { orgLink } };

        (await ReadCounts(await client.PostAsJsonAsync("/api/short-urls/bulk/deactivate", ids))).Should().Be((1, 0, 1));
        (await ReadCounts(await client.SendAsync(DeleteWithBody("/api/short-urls/bulk", ids)))).Should().Be((1, 0, 1));
        (await Seed.GetLinkAsync(orgLink))!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task BulkOperations_ByUserWithPersonalToken_DoNotAffectOrganizationLinks()
    {
        var (_, userId, _, orgLink) = await OrgWithMemberAndLinkAsync();

        var response = await As(userId).PostAsJsonAsync("/api/short-urls/bulk/deactivate", new { shortUrlIds = new[] { orgLink } });

        (await ReadCounts(response)).Should().Be((1, 0, 1));
        (await Seed.GetLinkAsync(orgLink))!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task BulkOperations_ByRemovedMemberWithStaleToken_DoNotAffectOrganizationLinks()
    {
        var (org, userId, memberId, orgLink) = await OrgWithMemberAndLinkAsync();
        var client = org.AsMember(userId, memberId, MemberRole.Member);
        await Seed.RemoveMemberAsync(org.OrganizationId, userId);
        var ids = new { shortUrlIds = new[] { orgLink } };

        (await ReadCounts(await client.PostAsJsonAsync("/api/short-urls/bulk/deactivate", ids))).Should().Be((1, 0, 1));
        (await ReadCounts(await client.SendAsync(DeleteWithBody("/api/short-urls/bulk", ids)))).Should().Be((1, 0, 1));
        (await Seed.GetLinkAsync(orgLink))!.IsActive.Should().BeTrue();
    }
}