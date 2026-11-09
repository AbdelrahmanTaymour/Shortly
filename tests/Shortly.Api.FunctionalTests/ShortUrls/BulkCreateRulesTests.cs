using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Api.FunctionalTests.ShortUrls;

[Collection(ApiCollection.Name)]
public class BulkCreateRulesTests(ApiFixture api) : ApiTestBase(api)
{
    private static object Items(int valid, int invalid = 0) => new
    {
        urls = Enumerable.Range(0, valid).Select(i => new { originalUrl = $"https://example.com/bulk/{Guid.NewGuid():N}/{i}" })
            .Concat(Enumerable.Range(0, invalid).Select(_ => new { originalUrl = "not a url" })).ToArray()
    };

    private static async Task<string?> ErrorCode(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString();

    private async Task<Guid> UserOnPlanAsync(enSubscriptionPlan plan, int usedThisMonth = 0)
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(user, plan);
        if (usedThisMonth > 0) await Seed.SetUserMonthlyLinksAsync(user, usedThisMonth);
        return user;
    }

    // ---- plan entitlement -----------------------------------------------------------------------------

    [Theory]
    [InlineData(enSubscriptionPlan.Free)]
    [InlineData(enSubscriptionPlan.Starter)]
    public async Task User_OnPlanWithoutBulkCreation_Gets403AndNothingIsCreated(enSubscriptionPlan plan)
    {
        var user = await UserOnPlanAsync(plan);

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 2));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(response)).Should().Be("ShortUrl.BulkCreationNotAllowed");
        (await Seed.CountUserLinksAsync(user)).Should().Be(0);
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(0);
    }

    [Theory]
    [InlineData(enSubscriptionPlan.Professional)]
    [InlineData(enSubscriptionPlan.Enterprise)]
    public async Task User_OnPlanWithBulkCreation_CreatesLinksAndUsageIncreasesByTheirCount(enSubscriptionPlan plan)
    {
        var user = await UserOnPlanAsync(plan, usedThisMonth: 10);

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 4));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var json = await ReadJsonAsync(response);
        json.GetProperty("successCount").GetInt32().Should().Be(4);
        json.GetProperty("createdIds").GetArrayLength().Should().Be(4);
        (await Seed.CountUserLinksAsync(user)).Should().Be(4);
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(14);
    }

    // ---- quota ----------------------------------------------------------------------------------------

    [Fact]
    public async Task User_ValidItemsExceedingRemainingQuota_RejectsTheWholeRequest()
    {
        var user = await UserOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 498); // 2 left of 500

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 3));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(response)).Should().Be("ShortUrl.MonthlyLinkLimitReached");
        (await Seed.CountUserLinksAsync(user)).Should().Be(0);
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(498);
    }

    [Fact]
    public async Task User_ValidItemsExactlyFillingQuota_AreAccepted()
    {
        var user = await UserOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 497);

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 3));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(500);
    }

    [Fact]
    public async Task User_RequestContainingAnInvalidUrl_IsRejectedByValidationAndCreatesNothing()
    {
        // Existing behavior, preserved: the request validator rejects the whole batch before any rule is applied.
        // (Per-item failures inside the handler, and "only valid items count", are covered by unit tests.)
        var user = await UserOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 10);

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 3, invalid: 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Seed.CountUserLinksAsync(user)).Should().Be(0);
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(10);
    }

    [Fact]
    public async Task User_AtQuota_CannotBulkCreateEvenOneLink()
    {
        var user = await UserOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 500);

        var response = await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 1));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.CountUserLinksAsync(user)).Should().Be(0);
    }

    [Fact]
    public async Task User_BulkCreationCountsTowardsTheSingleCreationQuota()
    {
        var user = await UserOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 497);
        (await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 3))).StatusCode.Should().Be(HttpStatusCode.OK);

        var single = await As(user).PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/after-bulk" });

        single.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(single)).Should().Be("ShortUrl.MonthlyLinkLimitReached");
    }

    [Fact]
    public async Task User_OnUnlimitedPlan_IsNeverQuotaLimited()
    {
        var user = await UserOnPlanAsync(enSubscriptionPlan.Enterprise, usedThisMonth: 5000);

        (await As(user).PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 20))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConcurrentRequests_CannotTogetherExceedTheQuota()
    {
        var user = await UserOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 495); // 5 left
        var first = As(user);
        var second = As(user);

        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 4)),
            second.PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 4)));

        var created = await Seed.CountUserLinksAsync(user);
        created.Should().BeLessOrEqualTo(5);
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(495 + created); // usage always matches what exists
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(created / 4);
    }

    [Fact]
    public async Task Anonymous_Gets401()
    {
        (await Anonymous().PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 1))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- organization ---------------------------------------------------------------------------------

    private async Task<(OrganizationFixture Org, HttpClient Member)> OrganizationOnPlanAsync(
        enSubscriptionPlan plan, int usedThisMonth = 0)
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, plan);
        if (usedThisMonth > 0) await Seed.SetOrganizationMonthlyLinksAsync(org.OrganizationId, usedThisMonth);
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        return (org, org.AsMember(userId, memberId, MemberRole.Member));
    }

    [Theory]
    [InlineData(enSubscriptionPlan.Free)]
    [InlineData(enSubscriptionPlan.Starter)]
    public async Task Organization_OnPlanWithoutBulkCreation_Gets403(enSubscriptionPlan plan)
    {
        var (org, member) = await OrganizationOnPlanAsync(plan);

        var response = await member.PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 2));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(response)).Should().Be("ShortUrl.BulkCreationNotAllowed");
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(0);
    }

    [Fact]
    public async Task Organization_OnProfessionalPlan_CreatesOrganizationOwnedLinksAndUsageIncreases()
    {
        var (org, member) = await OrganizationOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 20);

        var response = await member.PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 5));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(5);
        (await Seed.GetOrganizationMonthlyLinksAsync(org.OrganizationId)).Should().Be(25);
    }

    [Fact]
    public async Task Organization_ValidItemsExceedingRemainingQuota_RejectsTheWholeRequest()
    {
        var (org, member) = await OrganizationOnPlanAsync(enSubscriptionPlan.Professional, usedThisMonth: 499);

        var response = await member.PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 2));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(response)).Should().Be("ShortUrl.MonthlyLinkLimitReached");
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(0);
        (await Seed.GetOrganizationMonthlyLinksAsync(org.OrganizationId)).Should().Be(499);
    }

    [Fact]
    public async Task Organization_Deactivated_Gets403AndNothingIsCreated()
    {
        var (org, member) = await OrganizationOnPlanAsync(enSubscriptionPlan.Professional);
        await Seed.DeactivateOrganizationAsync(org.OrganizationId);

        var response = await member.PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 2));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(0);
    }

    [Fact]
    public async Task Organization_RemovedMemberWithStaleToken_Gets403EvenOnAProfessionalPlan()
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, enSubscriptionPlan.Professional);
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        var staleToken = org.AsMember(userId, memberId, MemberRole.Member);
        await Seed.RemoveMemberAsync(org.OrganizationId, userId);

        var response = await staleToken.PostAsJsonAsync("/api/short-urls/bulk", Items(valid: 2));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(0);
    }
}
