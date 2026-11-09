using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Analytics;

[Collection(ApiCollection.Name)]
public class AnalyticsEntitlementTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly string Range =
        $"from={Uri.EscapeDataString(DateTime.UtcNow.AddDays(-800).ToString("O"))}&to={Uri.EscapeDataString(DateTime.UtcNow.AddDays(2).ToString("O"))}";

    private static readonly string[] GeoEndpoints = ["geography", "devices"];
    private static readonly string[] AnalyticsEndpoints = ["overview", "engagement", "timeseries", "traffic"];

    private static string Path(string endpoint) => endpoint == "timeseries" ? $"timeseries?{Range}" : endpoint;

    private static HttpStatusCode Expected(enSubscriptionPlan plan, string endpoint) =>
        GeoEndpoints.Contains(endpoint)
            ? plan == enSubscriptionPlan.Enterprise ? HttpStatusCode.OK : HttpStatusCode.Forbidden
            : plan == enSubscriptionPlan.Free ? HttpStatusCode.Forbidden : HttpStatusCode.OK;

    private static string ExpectedCode(enSubscriptionPlan plan, string endpoint) =>
        plan == enSubscriptionPlan.Free ? "ShortUrl.LinkAnalysisNotAllowed" : "ShortUrl.GeoDeviceTrackingNotAllowed";

    public static IEnumerable<object[]> PlanByEndpoint(string[] endpoints) =>
        Enum.GetValues<enSubscriptionPlan>().SelectMany(plan => endpoints.Select(e => new object[] { plan, e }));

    public static IEnumerable<object[]> PerUrlMatrix => PlanByEndpoint([.. AnalyticsEndpoints, .. GeoEndpoints]);
    public static IEnumerable<object[]> MineMatrix => PlanByEndpoint([.. AnalyticsEndpoints, .. GeoEndpoints, "top-urls"]);

    private async Task<(Guid UserId, long LinkId)> UserWithLinkAsync(enSubscriptionPlan plan)
    {
        var user = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(user, plan);
        return (user, await Seed.CreateUserLinkAsync(user));
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString();

    private static long Sum(JsonElement json, string array, string property = "clicks") =>
        json.GetProperty(array).EnumerateArray().Sum(i => i.GetProperty(property).GetInt64());


    [Theory]
    [MemberData(nameof(PerUrlMatrix))]
    public async Task PerUrlAnalytics_FollowThePlanOfTheLinkOwner(enSubscriptionPlan plan, string endpoint)
    {
        var (user, linkId) = await UserWithLinkAsync(plan);

        var response = await As(user).GetAsync($"/api/analytics/urls/{linkId}/{Path(endpoint)}");

        response.StatusCode.Should().Be(Expected(plan, endpoint), await response.Content.ReadAsStringAsync());
        if (response.StatusCode == HttpStatusCode.Forbidden) (await ErrorCode(response)).Should().Be(ExpectedCode(plan, endpoint));
    }

    [Theory]
    [MemberData(nameof(MineMatrix))]
    public async Task MineAnalytics_FollowThePlanOfThePersonalScope(enSubscriptionPlan plan, string endpoint)
    {
        var (user, _) = await UserWithLinkAsync(plan);
        var path = endpoint == "timeseries" ? $"timeseries?{Range}" : endpoint;

        var response = await As(user).GetAsync($"/api/analytics/mine/{path}");

        response.StatusCode.Should().Be(endpoint == "top-urls"
            ? Expected(plan, "overview")
            : Expected(plan, endpoint), await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BasicLinkData_IsAvailableOnTheFreePlan()
    {
        var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Free);
        await Seed.SetTotalClicksAsync(linkId, 3);
        var client = As(user);

        var summary = await client.GetAsync("/api/analytics/mine/summary");
        summary.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(summary)).GetProperty("totalClicks").GetInt64().Should().Be(3);

        (await client.GetAsync("/api/analytics/approaching-limit?warningThreshold=0")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(await client.GetAsync($"/api/short-urls/{linkId}"))).GetProperty("totalClicks").GetInt32().Should().Be(3);
    }

    // ---- organization-owned data uses the organization's plan -------------------------------------------------------

    [Fact]
    public async Task OrganizationLink_UsesTheOrganizationsPlan_NotTheMembersPersonalPlan()
    {
        var org = await CreateOrganizationAsync();                        // Free
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        await Seed.SetUserPlanAsync(userId, enSubscriptionPlan.Enterprise);
        var ownerMemberId = await Seed.GetMemberIdAsync(org.OrganizationId, org.OwnerId);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, ownerMemberId);
        var member = org.AsMember(userId, memberId, MemberRole.Member);

        (await member.GetAsync($"/api/analytics/urls/{linkId}/overview")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await member.GetAsync("/api/analytics/mine/overview")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The same person in personal context is judged by their own (Enterprise) plan.
        (await As(userId).GetAsync("/api/analytics/mine/overview")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(enSubscriptionPlan.Starter, "overview", HttpStatusCode.OK)]
    [InlineData(enSubscriptionPlan.Starter, "geography", HttpStatusCode.Forbidden)]
    [InlineData(enSubscriptionPlan.Enterprise, "geography", HttpStatusCode.OK)]
    public async Task OrganizationLink_IsAllowedWhenTheOrganizationsPlanIncludesIt_EvenIfTheMemberIsOnFree(
        enSubscriptionPlan orgPlan, string endpoint, HttpStatusCode expected)
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, orgPlan);
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Viewer);        // personal plan: Free
        var ownerMemberId = await Seed.GetMemberIdAsync(org.OrganizationId, org.OwnerId);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, ownerMemberId);
        var member = org.AsMember(userId, memberId, MemberRole.Viewer);

        (await member.GetAsync($"/api/analytics/urls/{linkId}/{endpoint}")).StatusCode.Should().Be(expected);
        (await member.GetAsync($"/api/analytics/mine/{endpoint}")).StatusCode.Should().Be(expected);
    }

    // ---- system administrators --------------------------------------------------------------------------------------

    [Theory]
    [InlineData("overview")]
    [InlineData("geography")]
    [InlineData("devices")]
    [InlineData("traffic")]
    public async Task SystemAdmin_BypassesEntitlements_ForAFreeOwnersLink(string endpoint)
    {
        var (_, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Free);

        var response = await As(Guid.NewGuid(), UserRole.SystemAdmin).GetAsync($"/api/analytics/urls/{linkId}/{endpoint}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemAdmin_BypassesRetentionAndSeesPartialFields_AnonymousLinksIncluded()
    {
        var (owner, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Starter);       // 30 days, no geo, no campaigns
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-1), country: "Egypt", utmCampaign: "spring");
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-200));
        var admin = As(Guid.NewGuid(), UserRole.SystemAdmin);

        var asAdmin = await ReadJsonAsync(await admin.GetAsync($"/api/analytics/urls/{linkId}/overview"));
        var asOwner = await ReadJsonAsync(await As(owner).GetAsync($"/api/analytics/urls/{linkId}/overview"));
        var traffic = await ReadJsonAsync(await admin.GetAsync($"/api/analytics/urls/{linkId}/traffic"));

        asAdmin.GetProperty("totalClicks").GetInt64().Should().Be(2);           // 200-day-old click visible to the admin
        asAdmin.GetProperty("topCountry").GetString().Should().Be("Egypt");
        asOwner.GetProperty("totalClicks").GetInt64().Should().Be(1);
        traffic.GetProperty("topCampaigns").GetArrayLength().Should().BeGreaterThan(0);

        var anonymousLink = await Seed.CreateAnonymousLinkAsync();
        (await admin.GetAsync($"/api/analytics/urls/{anonymousLink}/overview")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- partial responses ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(enSubscriptionPlan.Starter, false, false)]
    [InlineData(enSubscriptionPlan.Professional, false, true)]
    [InlineData(enSubscriptionPlan.Enterprise, true, true)]
    public async Task GatedFields_AreOmittedWithoutFailingTheEndpoint(enSubscriptionPlan plan, bool geo, bool campaigns)
    {
        var (user, linkId) = await UserWithLinkAsync(plan);
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-1), country: "Egypt", utmCampaign: "spring");
        var client = As(user);

        var overview = await client.GetAsync($"/api/analytics/urls/{linkId}/overview");
        var traffic = await client.GetAsync($"/api/analytics/urls/{linkId}/traffic");
        var topUrls = await client.GetAsync("/api/analytics/mine/top-urls");

        overview.StatusCode.Should().Be(HttpStatusCode.OK);
        traffic.StatusCode.Should().Be(HttpStatusCode.OK);
        topUrls.StatusCode.Should().Be(HttpStatusCode.OK);

        var overviewJson = await ReadJsonAsync(overview);
        overviewJson.GetProperty("totalClicks").GetInt64().Should().Be(1);        // the rest of the report is intact
        (overviewJson.GetProperty("topCountry").ValueKind == JsonValueKind.String).Should().Be(geo);

        var campaignCount = (await ReadJsonAsync(traffic)).GetProperty("topCampaigns").GetArrayLength();
        (campaignCount > 0).Should().Be(campaigns);

        var topCountry = (await ReadJsonAsync(topUrls)).EnumerateArray().Single().GetProperty("topCountry");
        (topCountry.ValueKind == JsonValueKind.String).Should().Be(geo);
    }

    // ---- retention boundaries -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(enSubscriptionPlan.Starter, 29.5, 30.5)]        // 30 days
    [InlineData(enSubscriptionPlan.Professional, 89.5, 90.5)]   // 90 days
    public async Task Retention_HidesClicksOlderThanThePlansWindow(enSubscriptionPlan plan, double insideDays, double outsideDays)
    {
        var (user, linkId) = await UserWithLinkAsync(plan);
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-1));
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-insideDays));
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-outsideDays));
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-400));
        var client = As(user);

        var perUrl = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview"));
        var mine = await ReadJsonAsync(await client.GetAsync("/api/analytics/mine/overview"));

        perUrl.GetProperty("totalClicks").GetInt64().Should().Be(2);
        mine.GetProperty("totalClicks").GetInt64().Should().Be(2);
    }

    [Fact]
    public async Task Retention_Unlimited_ShowsAllHistory()
    {
        var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Enterprise);   // -1
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-1));
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-700));

        var overview = await ReadJsonAsync(await As(user).GetAsync($"/api/analytics/urls/{linkId}/overview"));

        overview.GetProperty("totalClicks").GetInt64().Should().Be(2);
    }

    [Fact]
    public async Task Retention_AnExplicitFromInsideTheWindowIsHonored_OneBeforeItIsClamped()
    {
        var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Starter);
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-1));
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-10));
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-20));
        var client = As(user);
        string From(int days) => Uri.EscapeDataString(DateTime.UtcNow.AddDays(-days).ToString("O"));

        var narrower = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview?from={From(5)}"));
        var wider = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview?from={From(500)}"));

        narrower.GetProperty("totalClicks").GetInt64().Should().Be(1);
        wider.GetProperty("totalClicks").GetInt64().Should().Be(3);        // clamped to 30 days, not 500
    }

    [Fact]
    public async Task Retention_AppliesToEveryClickAnalyticsEndpoint_ViaTheResolvedScope()
    {
        // Enterprise is unlimited in the seed data; give it 30 days (restored afterwards) so geography and devices can be checked too.
        var original = await Seed.GetPlanRetentionAsync(enSubscriptionPlan.Enterprise);
        await Seed.SetPlanRetentionAsync(enSubscriptionPlan.Enterprise, 30);
        try
        {
            var factory = Api.CreateFactory();
            var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Enterprise);
            var sessionNew = Guid.NewGuid().ToString("N");
            var sessionOld = Guid.NewGuid().ToString("N");
            await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-2), sessionNew, "Egypt", "Mobile", "spring");
            await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-100), sessionOld, "France", "Desktop", "winter");
            var client = factory.CreateApiClient(TestTokens.Create(user));

            var overview = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview"));
            var geography = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/geography"));
            var devices = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/devices"));
            var traffic = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/traffic"));
            var engagement = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/engagement"));
            var series = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/timeseries?{Range}"));
            var topUrls = await ReadJsonAsync(await client.GetAsync("/api/analytics/mine/top-urls"));

            overview.GetProperty("totalClicks").GetInt64().Should().Be(1);
            Sum(geography, "topCountries").Should().Be(1);
            geography.GetProperty("topCountries").EnumerateArray().Select(c => c.GetProperty("name").GetString()).Should().Equal("Egypt");
            Sum(devices, "topDeviceTypes").Should().Be(1);
            Sum(traffic, "topTrafficSources").Should().Be(1);
            traffic.GetProperty("topCampaigns").EnumerateArray().Select(c => c.GetProperty("campaignName").GetString()).Should().Equal("spring");
            (engagement.GetProperty("newVisitors").GetInt32() + engagement.GetProperty("returningVisitors").GetInt32()).Should().Be(1);
            series.GetProperty("trend").EnumerateArray().Sum(p => p.GetProperty("clicks").GetInt64()).Should().Be(1);
            topUrls.EnumerateArray().Single().GetProperty("topCountry").GetString().Should().Be("Egypt");
            topUrls.EnumerateArray().Single().GetProperty("lastClickAtUtc").GetDateTime().Should().BeAfter(DateTime.UtcNow.AddDays(-30));
        }
        finally
        {
            await Seed.SetPlanRetentionAsync(enSubscriptionPlan.Enterprise, original);
        }
    }

    [Fact]
    public async Task Retention_Zero_MeansNoHistoricalClickAnalytics_ButBasicCountersAreUntouched()
    {
        var original = await Seed.GetPlanRetentionAsync(enSubscriptionPlan.Enterprise);
        await Seed.SetPlanRetentionAsync(enSubscriptionPlan.Enterprise, 0);
        try
        {
            var factory = Api.CreateFactory();
            var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Enterprise);
            await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddMinutes(-5), country: "Egypt");
            await Seed.SetTotalClicksAsync(linkId, 4);
            var client = factory.CreateApiClient(TestTokens.Create(user));

            var overview = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview"));
            var geography = await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/geography"));
            var topUrls = await ReadJsonAsync(await client.GetAsync("/api/analytics/mine/top-urls"));
            var summary = await ReadJsonAsync(await client.GetAsync("/api/analytics/mine/summary"));
            var link = await ReadJsonAsync(await client.GetAsync($"/api/short-urls/{linkId}"));

            overview.GetProperty("totalClicks").GetInt64().Should().Be(0);
            geography.GetProperty("topCountries").GetArrayLength().Should().Be(0);
            topUrls.EnumerateArray().Single().GetProperty("lastClickAtUtc").ValueKind.Should().Be(JsonValueKind.Null);

            // Counters and usage summaries are not click-event history and are never clamped.
            summary.GetProperty("totalClicks").GetInt64().Should().Be(4);
            link.GetProperty("totalClicks").GetInt32().Should().Be(4);
        }
        finally
        {
            await Seed.SetPlanRetentionAsync(enSubscriptionPlan.Enterprise, original);
        }
    }

    // ---- downgrade and upgrade are reversible -----------------------------------------------------------------------------

    [Fact]
    public async Task Downgrade_DeniesAnalytics_ButStoredDataSurvivesAnUpgrade()
    {
        var (user, linkId) = await UserWithLinkAsync(enSubscriptionPlan.Professional);
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-1));
        await Seed.AddClickAsync(linkId, DateTime.UtcNow.AddDays(-70));
        var client = As(user);
        (await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview"))).GetProperty("totalClicks").GetInt64().Should().Be(2);

        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Free);
        (await client.GetAsync($"/api/analytics/urls/{linkId}/overview")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Professional);
        (await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview"))).GetProperty("totalClicks").GetInt64().Should().Be(2);

        // A shorter window hides the older click without deleting it; a longer one shows it again.
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Starter);
        (await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview"))).GetProperty("totalClicks").GetInt64().Should().Be(1);
        await Seed.SetUserPlanAsync(user, enSubscriptionPlan.Enterprise);
        (await ReadJsonAsync(await client.GetAsync($"/api/analytics/urls/{linkId}/overview"))).GetProperty("totalClicks").GetInt64().Should().Be(2);
    }

    [Fact]
    public async Task ClickCapture_IsUnchangedForUnentitledOwners()
    {
        // Entitlements are applied when reading: a Free owner's tracked clicks are still recorded.
        var owner = await Seed.CreateUserAsync();
        var linkId = await Seed.CreateUserLinkAsync(owner);
        var link = (await Seed.GetLinkAsync(linkId))!;

        (await Anonymous().GetAsync($"/{link.ShortCode}?utm_campaign=freebie")).StatusCode.Should().Be(HttpStatusCode.Redirect);

        var recorded = false;
        for (var i = 0; i < 50 && !recorded; i++)
        {
            await Task.Delay(100);
            recorded = await Seed.CountClickEventsAsync(linkId) > 0;
        }
        recorded.Should().BeTrue();
    }
}
