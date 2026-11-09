using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.ShortUrls;

[Collection(ApiCollection.Name)]
public class LinkProtectionEntitlementTests(ApiFixture api) : ApiTestBase(api)
{
    private const string Password = "a-strong-password-1";

    private static async Task<string?> ErrorCode(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("errorCode").GetString();

    private async Task<(Guid UserId, long LinkId)> UserWithLinkAsync(enSubscriptionPlan plan)
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(user, plan);
        return (user, await Seed.CreateUserLinkAsync(user));
    }

    // ---- user-owned links: every plan ------------------------------------------------------------------

    [Theory]
    [InlineData(enSubscriptionPlan.Free, false)]
    [InlineData(enSubscriptionPlan.Starter, false)]
    [InlineData(enSubscriptionPlan.Professional, true)]
    [InlineData(enSubscriptionPlan.Enterprise, true)]
    public async Task SetPassword_OnUserOwnedLink_FollowsTheOwnersPlan(enSubscriptionPlan plan, bool allowed)
    {
        var (user, linkId) = await UserWithLinkAsync(plan);

        var response = await As(user).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password });

        response.StatusCode.Should().Be(allowed ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        if (!allowed) (await ErrorCode(response)).Should().Be("ShortUrl.LinkProtectionNotAllowed");
        (await Seed.GetLinkAsync(linkId))!.IsPasswordProtected.Should().Be(allowed);
    }

    // ---- organization-owned links: the organization's plan, never the member's -------------------------

    [Fact]
    public async Task OrganizationLink_UsesTheOrganizationsPlan_EvenWhenTheMembersPersonalPlanIsHigher()
    {
        var org = await CreateOrganizationAsync();                  // organization is on Free
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        await Seed.SetUserPlanAsync(userId, enSubscriptionPlan.Enterprise);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId);

        var response = await org.AsMember(userId, memberId, MemberRole.Member)
            .PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetLinkAsync(linkId))!.IsPasswordProtected.Should().BeFalse();
    }

    [Fact]
    public async Task OrganizationLink_IsAllowed_WhenTheOrganizationsPlanIncludesIt_EvenIfTheMemberIsOnFree()
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, enSubscriptionPlan.Professional);
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);   // personal plan: Free
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId);

        var response = await org.AsMember(userId, memberId, MemberRole.Member)
            .PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetLinkAsync(linkId))!.IsPasswordProtected.Should().BeTrue();
    }

    // ---- anonymous links are Free ------------------------------------------------------------------------

    [Fact]
    public async Task AnonymousLink_IsTreatedAsFree_EvenForASystemAdministrator()
    {
        var linkId = await Seed.CreateAnonymousLinkAsync();

        var response = await As(Guid.NewGuid(), UserRole.SystemAdmin)
            .PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetLinkAsync(linkId))!.IsPasswordProtected.Should().BeFalse();
    }

    // ---- removing protection is always allowed ----------------------------------------------------------

    [Fact]
    public async Task RemovePassword_IsAllowedOnAnyPlan()
    {
        var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Professional);
        await As(user).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password });
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Free);

        var response = await As(user).DeleteAsync($"/api/short-urls/{linkId}/password");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetLinkAsync(linkId))!.IsPasswordProtected.Should().BeFalse();
    }

    // ---- downgrade ----------------------------------------------------------------------------------------

    [Fact]
    public async Task AfterADowngrade_TheLinkStillRequiresItsPassword_ButItCannotBeChanged()
    {
        var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Professional);
        (await As(user).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = Password })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        var link = (await Seed.GetLinkAsync(linkId))!;
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Free);

        // Visitors still hit the password page, not the destination.
        var visit = await Anonymous().GetAsync($"/{link.ShortCode}");
        visit.StatusCode.Should().Be(HttpStatusCode.Redirect);
        visit.Headers.Location!.ToString().Should().Contain("password-page");

        // The owner can no longer change the password ...
        (await As(user).PostAsJsonAsync($"/api/short-urls/{linkId}/password", new { password = "another-password-2" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // ... but editing other properties keeps the protection in place.
        var update = await As(user).PutAsJsonAsync($"/api/short-urls/{linkId}", new
        {
            originalUrl = link.OriginalUrl, shortCode = link.ShortCode, title = "renamed", description = "x",
            clickLimit = 10, isActive = true, isPrivate = false, trackingEnabled = true, expiresAt = (DateTime?)null
        });
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());
        (await Seed.GetLinkAsync(linkId))!.IsPasswordProtected.Should().BeTrue();
    }

    // ---- unrelated link operations are unchanged for unentitled users -----------------------------------

    [Fact]
    public async Task FreeUser_CanStillManageTheirLinksInOtherWays()
    {
        var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Free);
        var client = As(user);

        (await client.PostAsync($"/api/short-urls/{linkId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/short-urls/{linkId}/activate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/short-urls/{linkId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/short-urls/mine")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/plain" })).StatusCode
            .Should().Be(HttpStatusCode.Created);
        (await client.DeleteAsync($"/api/short-urls/{linkId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}