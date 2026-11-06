using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// Creating or changing a custom short code needs the CustomShortCode feature of the link's OWNER (Starter and above).
/// Anonymous callers are Free. Editing other properties, and existing codes, are unaffected.
/// </summary>
[Collection(ApiCollection.Name)]
public class CustomShortCodeEntitlementTests(ApiFixture api) : ApiTestBase(api)
{
    private static string NewCode() => $"c{Guid.NewGuid():N}"[..12];

    private static async Task<string?> ErrorCode(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("errorCode").GetString();

    private static object Update(Seeder.LinkState link, string shortCode, string title = "edited") => new
    {
        originalUrl = link.OriginalUrl, shortCode, title, description = "x", clickLimit = 10,
        isActive = true, isPrivate = false, trackingEnabled = true, expiresAt = (DateTime?)null
    };

    private async Task<Guid> UserOnAsync(enSubscriptionPlan plan)
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(user, plan);
        return user;
    }

    // ---- create -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(enSubscriptionPlan.Free, false)]
    [InlineData(enSubscriptionPlan.Starter, true)]
    [InlineData(enSubscriptionPlan.Professional, true)]
    [InlineData(enSubscriptionPlan.Enterprise, true)]
    public async Task Create_WithCustomCode_FollowsTheOwnersPlan(enSubscriptionPlan plan, bool allowed)
    {
        var user = await UserOnAsync(plan);
        var code = NewCode();

        var response = await As(user).PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/c", customShortCode = code });

        response.StatusCode.Should().Be(allowed ? HttpStatusCode.Created : HttpStatusCode.Forbidden);
        if (!allowed) (await ErrorCode(response)).Should().Be("ShortUrl.CustomShortCodeNotAllowed");
        (await Seed.CountUserLinksAsync(user)).Should().Be(allowed ? 1 : 0);
        if (allowed) (await As(user).GetAsync($"/api/short-urls/{code}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithoutCustomCode_StillWorksOnFree_AndDoesNotCountAsACustomCodeRequest()
    {
        var user = await UserOnAsync(enSubscriptionPlan.Free);
        var client = As(user);

        (await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/a" })).StatusCode
            .Should().Be(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/b", customShortCode = (string?)null }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // Blank is "no custom code" and keeps its previous (validation) outcome instead of becoming an entitlement error.
        var blank = await client.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/c", customShortCode = "  " });
        blank.StatusCode.Should().NotBe(HttpStatusCode.Created);
        (await ErrorCode(blank)).Should().NotBe("ShortUrl.CustomShortCodeNotAllowed");
    }

    [Fact]
    public async Task Create_ByAnonymousCaller_WithCustomCode_IsDenied_ButWithoutItIsAllowed()
    {
        var code = NewCode();

        var denied = await Anonymous().PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/anon", customShortCode = code });
        var allowed = await Anonymous().PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/anon2" });

        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(denied)).Should().Be("ShortUrl.CustomShortCodeNotAllowed");
        allowed.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Anonymous().GetAsync($"/{code}")).StatusCode.Should().Be(HttpStatusCode.NotFound); // the code was not reserved
    }

    [Fact]
    public async Task Create_InOrganizationContext_UsesTheOrganizationsPlan_NotThePersonalOne()
    {
        var org = await CreateOrganizationAsync();                      // Free
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        await Seed.SetUserPlanAsync(userId, enSubscriptionPlan.Enterprise);
        var member = org.AsMember(userId, memberId, MemberRole.Member);

        (await member.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/o", customShortCode = NewCode() }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await Seed.SetOrganizationPlanAsync(org.OrganizationId, enSubscriptionPlan.Starter);
        (await member.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/o", customShortCode = NewCode() }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // A member on Free is fine when the organization is entitled.
        var (freeUser, freeMember) = await org.AddMemberAsync(MemberRole.Member);
        (await org.AsMember(freeUser, freeMember, MemberRole.Member)
            .PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/o", customShortCode = NewCode() }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- PATCH short-code -------------------------------------------------------------------------------

    [Theory]
    [InlineData(enSubscriptionPlan.Free, false)]
    [InlineData(enSubscriptionPlan.Starter, true)]
    [InlineData(enSubscriptionPlan.Enterprise, true)]
    public async Task AssignShortCode_FollowsTheOwnersPlan(enSubscriptionPlan plan, bool allowed)
    {
        var user = await UserOnAsync(plan);
        var linkId = await Seed.CreateUserLinkAsync(user);
        var original = (await Seed.GetLinkAsync(linkId))!.ShortCode;
        var code = NewCode();

        var response = await As(user).PatchAsJsonAsync($"/api/short-urls/{linkId}/short-code", new { newShortCode = code });

        response.StatusCode.Should().Be(allowed ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        (await Seed.GetLinkAsync(linkId))!.ShortCode.Should().Be(allowed ? code : original);
    }

    [Fact]
    public async Task AssignShortCode_OnOrganizationLink_UsesTheOrganizationsPlan()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        await Seed.SetUserPlanAsync(userId, enSubscriptionPlan.Enterprise);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId);

        (await org.AsMember(userId, memberId, MemberRole.Member)
            .PatchAsJsonAsync($"/api/short-urls/{linkId}/short-code", new { newShortCode = NewCode() }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- PUT: only a changed code is gated -----------------------------------------------------------------

    [Fact]
    public async Task Update_WithTheSameCode_IsAllowedOnFree()
    {
        var user = await UserOnAsync(enSubscriptionPlan.Free);
        var linkId = await Seed.CreateUserLinkAsync(user);
        var link = (await Seed.GetLinkAsync(linkId))!;

        var response = await As(user).PutAsJsonAsync($"/api/short-urls/{linkId}", Update(link, link.ShortCode, title: "new title"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(enSubscriptionPlan.Free, false)]
    [InlineData(enSubscriptionPlan.Starter, true)]
    public async Task Update_WithAChangedCode_FollowsTheOwnersPlan(enSubscriptionPlan plan, bool allowed)
    {
        var user = await UserOnAsync(plan);
        var linkId = await Seed.CreateUserLinkAsync(user);
        var link = (await Seed.GetLinkAsync(linkId))!;
        var code = NewCode();

        var response = await As(user).PutAsJsonAsync($"/api/short-urls/{linkId}", Update(link, code));

        response.StatusCode.Should().Be(allowed ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
        (await Seed.GetLinkAsync(linkId))!.ShortCode.Should().Be(allowed ? code : link.ShortCode);
    }

    // ---- downgrade ----------------------------------------------------------------------------------------

    [Fact]
    public async Task AfterADowngrade_ExistingCustomCodesKeepWorking_ButCannotBeChanged()
    {
        var user = await UserOnAsync(enSubscriptionPlan.Starter);
        var code = NewCode();
        (await As(user).PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/keep", customShortCode = code }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Free);
        var link = (await Seed.GetLinkAsync((await ReadJsonAsync(await As(user).GetAsync($"/api/short-urls/{code}"))).GetProperty("id").GetInt64()))!;

        // The code still redirects.
        var visit = await Anonymous().GetAsync($"/{code}");
        visit.StatusCode.Should().Be(HttpStatusCode.Redirect);
        visit.Headers.Location!.ToString().Should().Be(link.OriginalUrl);

        // Other properties can still be edited with the code unchanged; the code itself cannot change.
        (await As(user).PutAsJsonAsync($"/api/short-urls/{link.Id}", Update(link, code, title: "still editable")))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await As(user).PatchAsJsonAsync($"/api/short-urls/{link.Id}/short-code", new { newShortCode = NewCode() }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetLinkAsync(link.Id))!.ShortCode.Should().Be(code);
    }

    [Fact]
    public async Task BulkCreate_GeneratesCodesAndIsNotAffectedByTheCustomCodeFeature()
    {
        var user = await UserOnAsync(enSubscriptionPlan.Professional); // has bulk creation, still has custom codes too
        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk",
            new { urls = new[] { new { originalUrl = "https://example.com/bulk-1" } } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
