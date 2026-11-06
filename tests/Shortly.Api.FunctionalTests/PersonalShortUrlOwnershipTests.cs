using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// Personal (user-owned) links: user B must not be able to read or manage user A's links,
/// by id or by short code. Requests use valid payloads so the ownership check, not validation, is what answers.
/// </summary>
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

/// <summary>An all-digit short code matches the {id:long} route; it must still be reachable by code, and only by its owner.</summary>
[Collection(ApiCollection.Name)]
public class NumericShortCodeTests(ApiFixture api) : ApiTestBase(api)
{
    private static string DigitCode() => (Random.Shared.NextInt64(10_000_000, 99_999_999)).ToString();

    [Fact]
    public async Task NumericShortCode_CanBeFetchedByCode_ByItsOwner()
    {
        var owner = await Seed.CreateUserAsync();
        var code = DigitCode();
        var linkId = await Seed.CreateUserLinkAsync(owner, shortCode: code);

        var response = await As(owner).GetAsync($"/api/short-urls/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("shortCode").GetString().Should().Be(code);
        json.GetProperty("id").GetInt64().Should().Be(linkId);
    }

    [Fact]
    public async Task NumericShortCode_WithLeadingZeros_KeepsTheExactText()
    {
        var owner = await Seed.CreateUserAsync();
        var code = "00" + DigitCode();
        await Seed.CreateUserLinkAsync(owner, shortCode: code);

        var response = await As(owner).GetAsync($"/api/short-urls/{code}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("shortCode").GetString().Should().Be(code);
    }

    [Fact]
    public async Task NumericShortCode_IsNotVisibleToAnotherUser()
    {
        var owner = await Seed.CreateUserAsync();
        var code = DigitCode();
        await Seed.CreateUserLinkAsync(owner, shortCode: code);

        (await As(await Seed.CreateUserAsync()).GetAsync($"/api/short-urls/{code}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LookupByRealId_StillWins_AndUnknownNumbersStay404()
    {
        var owner = await Seed.CreateUserAsync();
        var linkId = await Seed.CreateUserLinkAsync(owner);
        var client = As(owner);

        (await ReadJsonAsync(await client.GetAsync($"/api/short-urls/{linkId}"))).GetProperty("id").GetInt64().Should().Be(linkId);
        (await client.GetAsync("/api/short-urls/987654321098")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
