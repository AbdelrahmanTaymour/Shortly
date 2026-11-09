using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.ShortUrls;

[Collection(ApiCollection.Name)]
public class PersonalShortUrlOwnershipTests(ApiFixture api) : ApiTestBase(api)
{
    private sealed record Scenario(Guid OwnerId, long LinkId, string ShortCode, Guid IntruderId);

    private async Task<Scenario> NewScenarioAsync()
    {
        var owner = await Seed.CreateUserAsync();
        var linkId = await Seed.CreateUserLinkAsync(owner);
        var link = await Seed.GetLinkAsync(linkId);
        return new Scenario(owner, linkId, link!.ShortCode, await Seed.CreateUserAsync());
    }

    private static object ValidUpdate(string shortCode) => new
    {
        originalUrl = "https://example.com/hijacked", shortCode, title = "x", description = "x",
        clickLimit = 10, isActive = true, isPrivate = false, trackingEnabled = true, expiresAt = (DateTime?)null
    };

    // ---- reads ---------------------------------------------------------------------------------

    [Fact]
    public async Task Intruder_CannotReadLinkById()
    {
        var s = await NewScenarioAsync();

        (await As(s.IntruderId).GetAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Intruder_CannotReadLinkByShortCode()
    {
        var s = await NewScenarioAsync();

        (await As(s.IntruderId).GetAsync($"/api/short-urls/{s.ShortCode}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Intruder_DoesNotSeeLinkInMineOrSearch()
    {
        var s = await NewScenarioAsync();

        var mine = await ReadJsonAsync(await As(s.IntruderId).GetAsync("/api/short-urls/mine"));
        var search = await ReadJsonAsync(await As(s.IntruderId).GetAsync("/api/short-urls/search?page=1&pageSize=50"));

        mine.GetProperty("totalCount").GetInt32().Should().Be(0);
        search.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // ---- writes: nothing may change -----------------------------------------------------------

    [Fact]
    public async Task Intruder_CannotUpdateLink()
    {
        var s = await NewScenarioAsync();

        var response = await As(s.IntruderId).PutAsJsonAsync($"/api/short-urls/{s.LinkId}", ValidUpdate(s.ShortCode));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Seed.GetLinkAsync(s.LinkId))!.OriginalUrl.Should().Be("https://example.com/functional-test");
    }

    [Fact]
    public async Task Intruder_CannotAssignShortCode()
    {
        var s = await NewScenarioAsync();

        var response = await As(s.IntruderId).PatchAsJsonAsync(
            $"/api/short-urls/{s.LinkId}/short-code", new { newShortCode = "stolen123" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Seed.GetLinkAsync(s.LinkId))!.ShortCode.Should().Be(s.ShortCode);
    }

    [Fact]
    public async Task Intruder_CannotDeactivateOrActivateLink()
    {
        var s = await NewScenarioAsync();

        (await As(s.IntruderId).PostAsync($"/api/short-urls/{s.LinkId}/deactivate", null)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await Seed.GetLinkAsync(s.LinkId))!.IsActive.Should().BeTrue();

        // The owner deactivates it, then the intruder tries to bring it back.
        (await As(s.OwnerId).PostAsync($"/api/short-urls/{s.LinkId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await As(s.IntruderId).PostAsync($"/api/short-urls/{s.LinkId}/activate", null)).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await Seed.GetLinkAsync(s.LinkId))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Intruder_CannotSetOrRemovePassword()
    {
        var s = await NewScenarioAsync();
        await Seed.SetUserPlanAsync(s.OwnerId, Shortly.Domain.SubscriptionPlan.enSubscriptionPlan.Professional); // owner may use link protection

        (await As(s.IntruderId).PostAsJsonAsync($"/api/short-urls/{s.LinkId}/password", new { password = "intruder-password-1" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Seed.GetLinkAsync(s.LinkId))!.IsPasswordProtected.Should().BeFalse();

        (await As(s.OwnerId).PostAsJsonAsync($"/api/short-urls/{s.LinkId}/password", new { password = "owner-password-1" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await As(s.IntruderId).DeleteAsync($"/api/short-urls/{s.LinkId}/password")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await Seed.GetLinkAsync(s.LinkId))!.IsPasswordProtected.Should().BeTrue();
    }

    [Fact]
    public async Task Intruder_CannotDeleteLink()
    {
        var s = await NewScenarioAsync();

        (await As(s.IntruderId).DeleteAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await Seed.LinkExistsAsync(s.LinkId)).Should().BeTrue();
    }

    [Fact]
    public async Task AnonymousCaller_CannotAccessLinkEndpoints()
    {
        var s = await NewScenarioAsync();

        (await Anonymous().GetAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Anonymous().DeleteAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Seed.LinkExistsAsync(s.LinkId)).Should().BeTrue();
    }

    // ---- positive controls --------------------------------------------------------------------

    [Fact]
    public async Task Owner_CanReadUpdateAndDeleteOwnLink()
    {
        var s = await NewScenarioAsync();
        var owner = As(s.OwnerId);

        (await owner.GetAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await owner.GetAsync($"/api/short-urls/{s.ShortCode}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await owner.PutAsJsonAsync($"/api/short-urls/{s.LinkId}", ValidUpdate(s.ShortCode));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());
        (await Seed.GetLinkAsync(s.LinkId))!.OriginalUrl.Should().Be("https://example.com/hijacked");

        (await owner.DeleteAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.LinkExistsAsync(s.LinkId)).Should().BeFalse();
    }

    [Fact]
    public async Task SystemAdmin_CanReadAndDeleteAnyLink()
    {
        var s = await NewScenarioAsync();
        var admin = As(Guid.NewGuid(), UserRole.SystemAdmin);

        (await admin.GetAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.DeleteAsync($"/api/short-urls/{s.LinkId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
