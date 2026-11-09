using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Api.FunctionalTests.ShortUrls;

[Collection(ApiCollection.Name)]
public class SingleCreateRulesTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly object Body = new { originalUrl = "https://example.com/single" };

    private static async Task<string?> ErrorCode(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("errorCode").GetString();

    // ---- personal ------------------------------------------------------------------------------

    [Fact]
    public async Task User_UnderQuota_CreatesLinkAndUsageIncreasesByOne()
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserMonthlyLinksAsync(user, 4); // Free plan allows 5 per month

        var response = await As(user).PostAsJsonAsync("/api/short-urls", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(5);
        (await Seed.CountUserLinksAsync(user)).Should().Be(1);
    }

    [Fact]
    public async Task User_AtQuota_IsRejectedAndNothingIsCreated()
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserMonthlyLinksAsync(user, 5);

        var response = await As(user).PostAsJsonAsync("/api/short-urls", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(response)).Should().Be("ShortUrl.MonthlyLinkLimitReached");
        (await Seed.GetUserMonthlyLinksAsync(user)).Should().Be(5);
        (await Seed.CountUserLinksAsync(user)).Should().Be(0);
    }

    [Fact]
    public async Task User_OnUnlimitedPlan_IsNeverQuotaLimited()
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Enterprise);
        await Seed.SetUserMonthlyLinksAsync(user, 2000);

        (await As(user).PostAsJsonAsync("/api/short-urls", Body)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---- organization --------------------------------------------------------------------------

    [Fact]
    public async Task OrganizationMember_UnderQuota_CreatesOrganizationLinkAndUsageIncreasesByOne()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        await Seed.SetOrganizationMonthlyLinksAsync(org.OrganizationId, 4);

        var response = await org.AsMember(userId, memberId, MemberRole.Member).PostAsJsonAsync("/api/short-urls", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Seed.GetOrganizationMonthlyLinksAsync(org.OrganizationId)).Should().Be(5);
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(1);
        (await Seed.CountUserLinksAsync(userId)).Should().Be(0); // owned by the organization, not the member
    }

    [Fact]
    public async Task OrganizationMember_AtQuota_IsRejectedAndNothingIsCreated()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        await Seed.SetOrganizationMonthlyLinksAsync(org.OrganizationId, 5);

        var response = await org.AsMember(userId, memberId, MemberRole.Member).PostAsJsonAsync("/api/short-urls", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCode(response)).Should().Be("ShortUrl.MonthlyLinkLimitReached");
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(0);
    }

    [Fact]
    public async Task OrganizationMember_InDeactivatedOrganization_IsRejectedAndNothingIsCreated()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        var client = org.AsMember(userId, memberId, MemberRole.Member);
        await Seed.DeactivateOrganizationAsync(org.OrganizationId);

        var response = await client.PostAsJsonAsync("/api/short-urls", Body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(0);
    }
}
