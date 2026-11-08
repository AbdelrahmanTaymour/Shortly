using Shortly.Core.Analytics.GetAnalyticsOverview;
using Shortly.Core.Analytics.GetDeviceStats;
using Shortly.Core.Analytics.GetEngagementMetrics;
using Shortly.Core.Analytics.GetGeographicalStats;
using Shortly.Core.Analytics.GetTimeSeriesStats;
using Shortly.Core.Analytics.GetTrafficStats;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Analytics;

/// <summary>The numbers the dashboards show, computed by the real SQL over seeded click events.</summary>
public class AnalyticsAggregationTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    // Enterprise: unlimited retention and every analytics feature, so only the aggregation is under test.
    private async Task<(User User, ShortUrl Link)> SetupAsync()
    {
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        return (user, link);
    }

    private static readonly DateTime Day1 = DateTime.UtcNow.Date.AddDays(-20);

    // ---- overview ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Overview_Should_CountTotalAndUniqueClicks_ActiveDays_AndFirstAndLastClick()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, Day1.AddHours(10), session: "s1");
        await Data.ClickAsync(link.Id, Day1.AddHours(11), session: "s1");
        await Data.ClickAsync(link.Id, Day1.AddDays(1).AddHours(9), session: "s2");

        // Act
        var result = (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Value;

        // Assert
        result.TotalClicks.Should().Be(3);
        result.UniqueClicks.Should().Be(2);
        result.ActiveDays.Should().Be(2);
        result.AverageClicksPerDay.Should().Be(1.5);
        result.FirstClickAtUtc.Should().BeCloseTo(Day1.AddHours(10), TimeSpan.FromSeconds(1));
        result.LastClickAtUtc.Should().BeCloseTo(Day1.AddDays(1).AddHours(9), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Overview_Should_BeEmptyAndSafe_WhenThereAreNoClicks()
    {
        // Arrange
        await SetupAsync();

        // Act
        var result = (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Value;

        // Assert
        result.TotalClicks.Should().Be(0);
        result.ActiveDays.Should().Be(0);
        result.AverageClicksPerDay.Should().Be(0);
        result.FirstClickAtUtc.Should().BeNull();
        result.TopCountry.Should().BeNull();
        result.TopReferrer.Should().BeNull();
    }

    [Fact]
    public async Task Overview_Should_CountTodayThisWeekAndThisMonth()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var times = new[] { today, weekStart.AddSeconds(-1), monthStart.AddSeconds(-1) };
        foreach (var t in times) await Data.ClickAsync(link.Id, t);

        // Act
        var result = (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Value;

        // Assert
        result.ClicksToday.Should().Be(times.Count(t => t.Date == today));
        result.ClicksThisWeek.Should().Be(times.Count(t => t >= weekStart));
        result.ClicksThisMonth.Should().Be(times.Count(t => t >= monthStart));
    }

    [Fact]
    public async Task Overview_Should_PickTheMostCommonCountryAndReferrer_ReadingBlanksAsNone()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, country: "Germany", referrerDomain: "news.example");
        await Data.ClickAsync(link.Id, country: "Egypt", referrerDomain: "blog.example");
        await Data.ClickAsync(link.Id, country: "Egypt", referrerDomain: "blog.example");
        await Data.ClickAsync(link.Id, country: " ", referrerDomain: " ");
        await Data.ClickAsync(link.Id, country: " ", referrerDomain: " ");

        // Act
        var result = (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Value;

        // Assert
        result.TopCountry.Should().Be("Egypt");
        result.TopReferrer.Should().Be("blog.example");
    }

    [Fact]
    public async Task Overview_Should_BreakTiesAlphabetically()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, country: "Spain");
        await Data.ClickAsync(link.Id, country: "France");

        // Act
        var result = (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Value;

        // Assert
        result.TopCountry.Should().Be("France");
    }

    [Fact]
    public async Task Overview_Should_LimitToOneLink_WhenAShortUrlIdIsGiven()
    {
        // Arrange
        var (user, link) = await SetupAsync();
        var other = await Data.UserLinkAsync(user.Id);
        await Data.ClickAsync(link.Id);
        await Data.ClickAsync(other.Id);
        await Data.ClickAsync(other.Id);

        // Act
        var one = (await Send(new GetAnalyticsOverviewQuery(link.Id, null, null))).Value;
        var all = (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Value;

        // Assert
        one.TotalClicks.Should().Be(1);
        all.TotalClicks.Should().Be(3);
    }

    [Fact]
    public async Task Overview_Should_UseAnInclusiveStartAndAnExclusiveEnd()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, Day1);
        await Data.ClickAsync(link.Id, Day1.AddDays(1));
        await Data.ClickAsync(link.Id, Day1.AddDays(2));

        // Act
        var result = (await Send(new GetAnalyticsOverviewQuery(null, Day1, Day1.AddDays(2)))).Value;

        // Assert: the click exactly at the end is left out
        result.TotalClicks.Should().Be(2);
    }

    [Fact]
    public async Task Overview_Should_NeverMixInAnotherUsersClicks()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        var other = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        await Data.ClickAsync(link.Id);
        await Data.ClickAsync((await Data.UserLinkAsync(other.Id)).Id);

        // Act
        var result = (await Send(new GetAnalyticsOverviewQuery(null, null, null))).Value;

        // Assert
        result.TotalClicks.Should().Be(1);
    }

    // ---- time series ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task TimeSeries_Should_GroupClicksPerDay_PerHour_AndPerWeekday()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, Day1.AddHours(10), session: "a");
        await Data.ClickAsync(link.Id, Day1.AddHours(10), session: "b");
        await Data.ClickAsync(link.Id, Day1.AddHours(14), session: "a");
        await Data.ClickAsync(link.Id, Day1.AddDays(1).AddHours(10), session: "c");

        // Act
        var result = (await Send(new GetTimeSeriesStatsQuery(null, Day1, Day1.AddDays(5)))).Value;

        // Assert
        result.BucketType.Should().Be("daily");
        result.Trend.Select(p => (p.BucketStart.Date, p.Clicks, p.UniqueClicks)).Should().Equal(
            (Day1.Date, 3L, 2L), (Day1.AddDays(1).Date, 1L, 1L));
        result.ClicksByHourOfDay.Should().BeEquivalentTo(new Dictionary<int, long> { [10] = 3, [14] = 1 });
        result.PeakHour.Should().Be(10);
        result.ClicksByDayOfWeek[Day1.DayOfWeek].Should().Be(3);
        result.ClicksByDayOfWeek[Day1.AddDays(1).DayOfWeek].Should().Be(1);
        result.PeakDay.Should().Be(Day1.DayOfWeek);
        result.AverageClicksPerDay.Should().Be(2);
    }

    [Fact]
    public async Task TimeSeries_Should_BeEmpty_WhenTheRangeHasNoClicks()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, Day1);

        // Act
        var result = (await Send(new GetTimeSeriesStatsQuery(null, Day1.AddDays(5), Day1.AddDays(6)))).Value;

        // Assert
        result.Trend.Should().BeEmpty();
        result.PeakHour.Should().BeNull();
        result.PeakDay.Should().BeNull();
        result.AverageClicksPerDay.Should().Be(0);
    }

    // ---- devices ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Devices_Should_ReportShareByDeviceType_BrowserAndSystem()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, deviceType: "Mobile", browser: "Safari", os: "iOS");
        await Data.ClickAsync(link.Id, deviceType: "Mobile", browser: "Chrome", os: "Android");
        await Data.ClickAsync(link.Id, deviceType: "Desktop", browser: "Chrome", os: "Windows");
        await Data.ClickAsync(link.Id, deviceType: "Tablet", browser: "Chrome", os: "iOS");

        // Act
        var result = (await Send(new GetDeviceStatsQuery(null, null, null))).Value;

        // Assert
        result.MobilePercentage.Should().Be(50);
        result.DesktopPercentage.Should().Be(25);
        result.TabletPercentage.Should().Be(25);
        result.TopDeviceTypes.Should().HaveCount(3);
        result.TopDeviceTypes.First().Should().Be(new DeviceBreakdown("Mobile", 2, 50));
        result.TopBrowsers.First().Should().Be(new DeviceBreakdown("Chrome", 3, 75));
        result.TopOperatingSystems.First().Should().Be(new DeviceBreakdown("iOS", 2, 50));
    }

    [Fact]
    public async Task Devices_Should_NameBlankValuesUnknown_AndKeepOnlyTheTopSix()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, browser: " ");
        for (var i = 0; i < 7; i++) await Data.ClickAsync(link.Id, browser: $"Browser{i}");
        await Data.ClickAsync(link.Id, browser: "Browser0");

        // Act
        var result = (await Send(new GetDeviceStatsQuery(null, null, null))).Value;

        // Assert
        result.TopBrowsers.Should().HaveCount(6);
        result.TopBrowsers.First().Name.Should().Be("Browser0");
        result.TopBrowsers.Select(b => b.Name).Should().NotContain("Unknown"); // one click each loses the tie-break
    }

    [Fact]
    public async Task Devices_Should_ReportZeroes_WhenThereAreNoClicks()
    {
        // Arrange
        await SetupAsync();

        // Act
        var result = (await Send(new GetDeviceStatsQuery(null, null, null))).Value;

        // Assert
        result.TopDeviceTypes.Should().BeEmpty();
        result.MobilePercentage.Should().Be(0);
    }

    // ---- geography -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Geography_Should_RankCountriesAndCities_WithShareAndDistinctTotals()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, country: "Egypt", city: "Cairo");
        await Data.ClickAsync(link.Id, country: "Egypt", city: "Cairo");
        await Data.ClickAsync(link.Id, country: "Egypt", city: "Giza");
        await Data.ClickAsync(link.Id, country: "Germany", city: "Berlin");
        await Data.ClickAsync(link.Id, country: " ", city: " ");

        // Act
        var result = (await Send(new GetGeographicalStatsQuery(null, null, null))).Value;

        // Assert
        result.TopCountries.Should().Equal(new GeoBreakdown("Egypt", 3, 60), new GeoBreakdown("Germany", 1, 20));
        result.TopCities.First().Should().Be(new GeoBreakdown("Cairo", 2, 40));
        result.TotalDistinctCountries.Should().Be(2);
        result.TotalDistinctCities.Should().Be(3);
    }

    [Fact]
    public async Task Geography_Should_KeepOnlyTheTopFiveCountries()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        for (var i = 0; i < 7; i++) await Data.ClickAsync(link.Id, country: $"Country{i}");

        // Act
        var result = (await Send(new GetGeographicalStatsQuery(null, null, null))).Value;

        // Assert
        result.TopCountries.Should().HaveCount(5);
        result.TotalDistinctCountries.Should().Be(7);
    }

    // ---- traffic ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Traffic_Should_ReportSourcesReferrersCampaigns_AndTheChannelShares()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, trafficSource: "Direct");
        await Data.ClickAsync(link.Id, trafficSource: "Direct");
        await Data.ClickAsync(link.Id, trafficSource: "Search", referrerDomain: "google.com");
        await Data.ClickAsync(link.Id, trafficSource: "Social", referrerDomain: "x.com", campaign: "spring", source: "twitter", medium: "social");

        // Act
        var result = (await Send(new GetTrafficStatsQuery(null, null, null))).Value;

        // Assert
        result.DirectPercentage.Should().Be(50);
        result.SearchPercentage.Should().Be(25);
        result.SocialPercentage.Should().Be(25);
        result.ReferralPercentage.Should().Be(0);
        result.TopTrafficSources.First().Should().Be(new DeviceBreakdown("Direct", 2, 50));
        result.TopReferrers.Select(r => r.Domain).Should().BeEquivalentTo("google.com", "x.com");
        result.TopCampaigns.Should().ContainSingle().Which.Should().Be(new CampaignBreakdown("spring", "twitter", "social", 1, 25));
    }

    [Fact]
    public async Task Traffic_Should_BeZeroAndEmpty_WhenThereAreNoClicks()
    {
        // Arrange
        await SetupAsync();

        // Act
        var result = (await Send(new GetTrafficStatsQuery(null, null, null))).Value;

        // Assert
        result.DirectPercentage.Should().Be(0);
        result.TopCampaigns.Should().BeEmpty();
        result.TopReferrers.Should().BeEmpty();
    }

    // ---- engagement --------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Engagement_Should_SeparateSingleClickAndMultiClickSessions()
    {
        // Arrange
        var (_, link) = await SetupAsync();
        await Data.ClickAsync(link.Id, Day1, session: "s1");
        await Data.ClickAsync(link.Id, Day1.AddMinutes(4), session: "s1");
        await Data.ClickAsync(link.Id, Day1.AddMinutes(10), session: "s1");
        await Data.ClickAsync(link.Id, Day1, session: "s2");
        await Data.ClickAsync(link.Id, Day1, session: "s3");
        await Data.ClickAsync(link.Id, Day1, session: "s4");
        await Data.ClickAsync(link.Id, Day1.AddMinutes(2), session: "s4");

        // Act
        var result = (await Send(new GetEngagementMetricsQuery(null, null, null))).Value;

        // Assert: s1 has three clicks, s4 two, s2 and s3 one each
        result.NewVisitors.Should().Be(2);
        result.ReturningVisitors.Should().Be(2);
        result.BounceRate.Should().Be(50);
        result.ReturnVisitorRate.Should().Be(50);
        result.ClicksPerSession.Should().Be(7 / 4.0);
        result.AverageSessionDuration.TotalSeconds.Should().BeApproximately((600 + 120) / 4.0, 0.5);
    }

    [Fact]
    public async Task Engagement_Should_BeZero_WhenThereAreNoClicks()
    {
        // Arrange
        await SetupAsync();

        // Act
        var result = (await Send(new GetEngagementMetricsQuery(null, null, null))).Value;

        // Assert
        result.BounceRate.Should().Be(0);
        result.NewVisitors.Should().Be(0);
        result.AverageSessionDuration.Should().Be(TimeSpan.Zero);
    }
}
