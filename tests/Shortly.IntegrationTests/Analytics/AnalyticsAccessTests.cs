using Shortly.Core.Analytics.GetAnalyticsOverview;
using Shortly.Core.Analytics.GetDeviceStats;
using Shortly.Core.Analytics.GetEngagementMetrics;
using Shortly.Core.Analytics.GetGeographicalStats;
using Shortly.Core.Analytics.GetTimeSeriesStats;
using Shortly.Core.Analytics.GetTopPerformingShortUrls;
using Shortly.Core.Analytics.GetTrafficStats;
using Shortly.Domain.Members;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Analytics;

/// <summary>Who may see which analytics: plan features, retention, ownership, organization and admin scopes.</summary>
public class AnalyticsAccessTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static readonly DateTime From = TestData.Now.AddYears(-1);
    private static readonly DateTime To = TestData.Now.AddDays(1);

    // ---- plan gating ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ClickAnalytics_Should_BeRefused_OnThePlansWithoutLinkAnalysis()
    {
        // Arrange
        var free = await Data.UserAsync();
        As(free.Id);

        // Act / Assert
        (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Error.Should().Be(ShortUrlErrors.LinkAnalysisNotAllowed);
        (await Send(new GetTimeSeriesStatsQuery(null, From, To))).Error.Should().Be(ShortUrlErrors.LinkAnalysisNotAllowed);
        (await Send(new GetTrafficStatsQuery(null, null, null))).Error.Should().Be(ShortUrlErrors.LinkAnalysisNotAllowed);
        (await Send(new GetEngagementMetricsQuery(null, null, null))).Error.Should().Be(ShortUrlErrors.LinkAnalysisNotAllowed);
        (await Send(new GetTopPerformingShortUrlsQuery(5, null))).Error.Should().Be(ShortUrlErrors.LinkAnalysisNotAllowed);
    }

    [Fact]
    public async Task GeoAndDeviceAnalytics_Should_NeedGeoDeviceTracking_WhichStarterAndProfessionalLack()
    {
        // Arrange
        var starter = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var professional = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        var enterprise = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);

        // Act / Assert
        foreach (var user in new[] { starter, professional })
        {
            As(user.Id);
            (await Send(new GetDeviceStatsQuery(null, null, null))).Error.Should().Be(ShortUrlErrors.GeoDeviceTrackingNotAllowed);
            (await Send(new GetGeographicalStatsQuery(null, null, null))).Error.Should().Be(ShortUrlErrors.GeoDeviceTrackingNotAllowed);
        }

        As(enterprise.Id);
        (await Send(new GetDeviceStatsQuery(null, null, null))).IsSuccess.Should().BeTrue();
        (await Send(new GetGeographicalStatsQuery(null, null, null))).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Overview_Should_HideTheTopCountry_WithoutGeoTracking_AndShowItWithIt()
    {
        // Arrange
        var starter = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var enterprise = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        foreach (var user in new[] { starter, enterprise })
            await Data.ClickAsync((await Data.UserLinkAsync(user.Id)).Id, country: "Egypt");

        // Act
        As(starter.Id);
        var onStarter = await Send(new GetAnalyticsOverviewQuery(null, null, null));
        As(enterprise.Id);
        var onEnterprise = await Send(new GetAnalyticsOverviewQuery(null, null, null));

        // Assert
        onStarter.Value.TotalClicks.Should().Be(1);
        onStarter.Value.TopCountry.Should().BeNull();
        onEnterprise.Value.TopCountry.Should().Be("Egypt");
    }

    [Fact]
    public async Task Traffic_Should_HideCampaigns_WithoutCampaignTracking_ButKeepTheRestOfTheReport()
    {
        // Arrange
        var starter = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var professional = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        foreach (var user in new[] { starter, professional })
            await Data.ClickAsync((await Data.UserLinkAsync(user.Id)).Id, campaign: "spring");

        // Act
        As(starter.Id);
        var onStarter = await Send(new GetTrafficStatsQuery(null, null, null));
        As(professional.Id);
        var onProfessional = await Send(new GetTrafficStatsQuery(null, null, null));

        // Assert
        onStarter.Value.TopCampaigns.Should().BeEmpty();
        onStarter.Value.TopTrafficSources.Should().NotBeEmpty();
        onProfessional.Value.TopCampaigns.Should().ContainSingle().Which.CampaignName.Should().Be("spring");
    }

    // ---- retention -----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Analytics_Should_OnlyCountClicksInsideThePlansRetentionWindow()
    {
        // Arrange: Starter keeps 30 days of click data
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var link = await Data.UserLinkAsync(user.Id);
        await Data.ClickAsync(link.Id, TestData.Now.AddDays(-5));
        await Data.ClickAsync(link.Id, TestData.Now.AddDays(-45));
        As(user.Id);

        // Act
        var all = await Send(new GetAnalyticsOverviewQuery(null, null, null));
        var askedForMore = await Send(new GetAnalyticsOverviewQuery(null, TestData.Now.AddYears(-1), null));

        // Assert
        all.Value.TotalClicks.Should().Be(1);
        askedForMore.Value.TotalClicks.Should().Be(1);
    }

    [Fact]
    public async Task Analytics_Should_CountEveryClick_OnAPlanWithUnlimitedRetention()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var link = await Data.UserLinkAsync(user.Id);
        await Data.ClickAsync(link.Id, TestData.Now.AddDays(-5));
        await Data.ClickAsync(link.Id, TestData.Now.AddDays(-400));
        As(user.Id);

        // Act
        var result = await Send(new GetAnalyticsOverviewQuery(null, null, null));

        // Assert
        result.Value.TotalClicks.Should().Be(2);
    }

    // ---- ownership and scope -------------------------------------------------------------------------------------------

    [Fact]
    public async Task LinkAnalytics_Should_ReturnNotFound_ForSomeoneElsesLink_AndForAnUnknownOne()
    {
        // Arrange
        var owner = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var stranger = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var link = await Data.UserLinkAsync(owner.Id);
        As(stranger.Id);

        // Act
        var foreign = await Send(new GetAnalyticsOverviewQuery(link.Id, null, null));
        var unknown = await Send(new GetAnalyticsOverviewQuery(long.MaxValue, null, null));

        // Assert: the same answer for both, so link ids cannot be probed
        foreign.Error.Should().Be(ShortUrlErrors.NotFound);
        unknown.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task LinkAnalytics_Should_BeGatedByTheLinkOwnersPlan_NotTheCallers()
    {
        // Arrange: an Enterprise admin looks at a Free user's link
        var freeOwner = await Data.UserAsync();
        var link = await Data.UserLinkAsync(freeOwner.Id);
        await Data.ClickAsync(link.Id);

        // Act
        As(freeOwner.Id);
        var asOwner = await Send(new GetAnalyticsOverviewQuery(link.Id, null, null));
        As(Caller.Admin());
        var asAdmin = await Send(new GetAnalyticsOverviewQuery(link.Id, null, null));

        // Assert
        asOwner.Error.Should().Be(ShortUrlErrors.LinkAnalysisNotAllowed);
        asAdmin.Value.TotalClicks.Should().Be(1);
    }

    [Fact]
    public async Task OrganizationAnalytics_Should_UseTheOrganizationsPlan_AndOnlyItsLinks()
    {
        // Arrange
        var org = await Data.OrganizationAsync(plan: enSubscriptionPlan.Enterprise);
        var (member, memberRow) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        var orgLink = await Data.OrganizationLinkAsync(org.Id, memberRow.Id);
        await Data.ClickAsync(orgLink.Id);
        await Data.ClickAsync(orgLink.Id);
        var personal = await Data.UserLinkAsync(member.Id);
        await Data.ClickAsync(personal.Id);

        // Act
        As(member.Id, org.Id);
        var inOrg = await Send(new GetAnalyticsOverviewQuery(null, null, null));
        As(member.Id);
        var inPersonal = await Send(new GetAnalyticsOverviewQuery(null, null, null));

        // Assert: the organization is Enterprise, the user is Free
        inOrg.Value.TotalClicks.Should().Be(2);
        inPersonal.Error.Should().Be(ShortUrlErrors.LinkAnalysisNotAllowed);
    }

    [Fact]
    public async Task OrganizationLinkAnalytics_Should_BeVisibleToMembers_ButNotToOutsiders()
    {
        // Arrange
        var org = await Data.OrganizationAsync(plan: enSubscriptionPlan.Enterprise);
        var (member, _) = await Data.MemberAsync(org.Id);
        var (removed, _) = await Data.MemberAsync(org.Id, isActive: false);
        var outsider = await Data.UserAsync();
        var link = await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id);

        // Act
        As(member.Id, org.Id);
        var asMember = await Send(new GetAnalyticsOverviewQuery(link.Id, null, null));
        As(removed.Id, org.Id);
        var asRemoved = await Send(new GetAnalyticsOverviewQuery(link.Id, null, null));
        As(outsider.Id);
        var asOutsider = await Send(new GetAnalyticsOverviewQuery(link.Id, null, null));

        // Assert
        asMember.IsSuccess.Should().BeTrue();
        asRemoved.IsFailure.Should().BeTrue();
        asOutsider.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task Analytics_Should_RefuseAStaleOrganizationClaim()
    {
        // Arrange
        var org = await Data.OrganizationAsync(plan: enSubscriptionPlan.Enterprise);
        var (removed, _) = await Data.MemberAsync(org.Id, isActive: false);
        As(removed.Id, org.Id);

        // Act
        var result = await Send(new GetAnalyticsOverviewQuery(null, null, null));

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}
