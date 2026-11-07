using Microsoft.EntityFrameworkCore;
using Shortly.Core.Analytics.GetShortUrlPortfolioSummary;
using Shortly.Core.Analytics.GetTopPerformingShortUrls;
using Shortly.Core.Analytics.GetUrlsApproachingClickLimit;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Analytics;

/// <summary>Link-level reports: the portfolio summary, the top links, and the links close to their click limit.</summary>
public class LinkPortfolioTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task Sql(FormattableString sql) => Seed(db => db.Database.ExecuteSqlInterpolatedAsync(sql));

    private Task SetClicks(long id, int clicks) => Sql($"UPDATE short_urls SET total_clicks = {clicks} WHERE id = {id}");

    // ---- portfolio summary (available on every plan) -------------------------------------------------------------------

    [Fact]
    public async Task Portfolio_Should_SummarizeThePersonalLinks_OnTheFreePlan()
    {
        // Arrange
        var user = await Data.UserAsync();
        var plain = await Data.UserLinkAsync(user.Id);
        var inactive = await Data.UserLinkAsync(user.Id);
        var expired = await Data.UserLinkAsync(user.Id);
        var secret = await Data.UserLinkAsync(user.Id);
        await Sql($"UPDATE short_urls SET is_active = FALSE WHERE id = {inactive.Id}");
        await Sql($"UPDATE short_urls SET expires_at = {TestData.Now.AddDays(-1)} WHERE id = {expired.Id}");
        await Sql($"UPDATE short_urls SET is_private = TRUE, is_password_protected = TRUE, password_hash = 'x' WHERE id = {secret.Id}");
        await SetClicks(plain.Id, 6);
        await SetClicks(secret.Id, 2);
        await Data.UserLinkAsync((await Data.UserAsync()).Id); // someone else's
        As(user.Id);

        // Act
        var result = (await Send(new GetShortUrlPortfolioSummaryQuery())).Value;

        // Assert
        result.TotalUrls.Should().Be(4);
        result.ActiveUrls.Should().Be(3);
        result.ExpiredUrls.Should().Be(1);
        result.PrivateUrls.Should().Be(1);
        result.PasswordProtectedUrls.Should().Be(1);
        result.TotalClicks.Should().Be(8);
        result.AverageClicksPerUrl.Should().Be(2);
    }

    [Fact]
    public async Task Portfolio_Should_BeZero_ForAUserWithoutLinks()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var result = (await Send(new GetShortUrlPortfolioSummaryQuery())).Value;

        // Assert
        result.TotalUrls.Should().Be(0);
        result.TotalClicks.Should().Be(0);
        result.AverageClicksPerUrl.Should().Be(0);
    }

    [Fact]
    public async Task Portfolio_Should_SummarizeTheOrganizationsLinks_ForAMember()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);
        await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id);
        await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id);
        await Data.UserLinkAsync(member.Id);

        // Act
        As(member.Id, org.Id);
        var inOrg = (await Send(new GetShortUrlPortfolioSummaryQuery())).Value;
        As(member.Id);
        var personal = (await Send(new GetShortUrlPortfolioSummaryQuery())).Value;

        // Assert
        inOrg.TotalUrls.Should().Be(2);
        personal.TotalUrls.Should().Be(1);
    }

    // ---- top performing --------------------------------------------------------------------------------------------------

    [Fact]
    public async Task TopPerforming_Should_RankActiveLinksByClicks_AndExposeTheirDetails()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var low = await Data.UserLinkAsync(user.Id);
        var high = await Data.UserLinkAsync(user.Id);
        var inactive = await Data.UserLinkAsync(user.Id);
        await SetClicks(low.Id, 2);
        await SetClicks(high.Id, 9);
        await SetClicks(inactive.Id, 50);
        await Sql($"UPDATE short_urls SET is_active = FALSE WHERE id = {inactive.Id}");
        await Data.ClickAsync(high.Id, country: "Egypt");
        await Data.ClickAsync(high.Id, country: "Egypt");
        await Data.ClickAsync(high.Id, country: "Germany");
        As(user.Id);

        // Act
        var result = (await Send(new GetTopPerformingShortUrlsQuery(10, null))).Value;

        // Assert
        result.Select(r => r.ShortUrlId).Should().Equal(high.Id, low.Id);
        result[0].TotalClicks.Should().Be(9);
        result[0].ShortCode.Should().Be(high.ShortCode.Value);
        result[0].TopCountry.Should().Be("Egypt");
        result[0].LastClickAtUtc.Should().NotBeNull();
        result[1].TopCountry.Should().BeNull();
    }

    [Fact]
    public async Task TopPerforming_Should_ClampTheCount()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        for (var i = 0; i < 3; i++) await Data.UserLinkAsync(user.Id);
        As(user.Id);

        // Act
        var tooSmall = (await Send(new GetTopPerformingShortUrlsQuery(0, null))).Value;
        var two = (await Send(new GetTopPerformingShortUrlsQuery(2, null))).Value;

        // Assert
        tooSmall.Should().HaveCount(1);
        two.Should().HaveCount(2);
    }

    [Fact]
    public async Task TopPerforming_Should_HideTheTopCountry_WithoutGeoTracking()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var link = await Data.UserLinkAsync(user.Id);
        await Data.ClickAsync(link.Id, country: "Egypt");
        As(user.Id);

        // Act
        var result = (await Send(new GetTopPerformingShortUrlsQuery(5, null))).Value;

        // Assert
        result.Single().TopCountry.Should().BeNull();
    }

    [Fact]
    public async Task TopPerforming_Should_RankByClicksInsideTheTimeframe_IncludingOldLinks()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var oldBusy = await Data.UserLinkAsync(user.Id);
        var newQuiet = await Data.UserLinkAsync(user.Id);
        var idle = await Data.UserLinkAsync(user.Id);
        await Sql($"UPDATE short_urls SET created_at_utc = {TestData.Now.AddDays(-40)}, total_clicks = 100 WHERE id = {oldBusy.Id}");
        await SetClicks(idle.Id, 500); // a lifetime favourite with nothing recent
        await Data.ClickAsync(idle.Id, TestData.Now.AddDays(-30));
        for (var i = 0; i < 3; i++) await Data.ClickAsync(oldBusy.Id, TestData.Now.AddHours(-i - 1));
        await Data.ClickAsync(newQuiet.Id, TestData.Now.AddHours(-1));
        await Data.ClickAsync(oldBusy.Id, TestData.Now.AddDays(-20)); // outside the window
        As(user.Id);

        // Act
        var result = (await Send(new GetTopPerformingShortUrlsQuery(5, TimeSpan.FromDays(7)))).Value;

        // Assert
        result.Select(r => (r.ShortUrlId, r.TotalClicks)).Should().Equal((oldBusy.Id, 3L), (newQuiet.Id, 1L));
    }

    [Fact]
    public async Task TopPerforming_Should_NotReachBeyondThePlansRetention_WhenTheTimeframeIsLonger()
    {
        // Arrange: Starter keeps 30 days
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var link = await Data.UserLinkAsync(user.Id);
        await Data.ClickAsync(link.Id, TestData.Now.AddDays(-5));
        await Data.ClickAsync(link.Id, TestData.Now.AddDays(-45));
        As(user.Id);

        // Act
        var result = (await Send(new GetTopPerformingShortUrlsQuery(5, TimeSpan.FromDays(90)))).Value;

        // Assert
        result.Single().TotalClicks.Should().Be(1);
    }

    // ---- approaching the click limit -------------------------------------------------------------------------------------

    [Fact]
    public async Task ApproachingLimit_Should_ListLinksAtOrAboveTheThreshold_MostUsedFirst()
    {
        // Arrange
        var user = await Data.UserAsync();
        var at80 = await Data.UserLinkAsync(user.Id, clickLimit: 10);
        var at95 = await Data.UserLinkAsync(user.Id, clickLimit: 20);
        var at70 = await Data.UserLinkAsync(user.Id, clickLimit: 10);
        var unlimited = await Data.UserLinkAsync(user.Id);
        await SetClicks(at80.Id, 8);
        await SetClicks(at95.Id, 19);
        await SetClicks(at70.Id, 7);
        await SetClicks(unlimited.Id, 1000);
        As(user.Id);

        // Act
        var result = (await Send(new GetUrlsApproachingClickLimitQuery(0.8, 1, 10))).Value;

        // Assert
        result.Items.Select(i => i.Id).Should().Equal(at95.Id, at80.Id);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task ApproachingLimit_Should_IncludeALinkThatReachedItsLimit_AndPageTheResults()
    {
        // Arrange
        var user = await Data.UserAsync();
        for (var i = 0; i < 3; i++)
        {
            var link = await Data.UserLinkAsync(user.Id, clickLimit: 5);
            await SetClicks(link.Id, 5);
        }
        As(user.Id);

        // Act
        var page1 = (await Send(new GetUrlsApproachingClickLimitQuery(0.9, 1, 2))).Value;
        var page2 = (await Send(new GetUrlsApproachingClickLimitQuery(0.9, 2, 2))).Value;

        // Assert
        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(1);
        page1.TotalCount.Should().Be(3);
        page1.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task ApproachingLimit_Should_ListOrganizationLinks_ForAMember_AndNotOthers()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);
        var orgLink = await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id, clickLimit: 10);
        await SetClicks(orgLink.Id, 9);
        var personal = await Data.UserLinkAsync(member.Id, clickLimit: 10);
        await SetClicks(personal.Id, 9);

        // Act
        As(member.Id, org.Id);
        var inOrg = (await Send(new GetUrlsApproachingClickLimitQuery(0.8, 1, 10))).Value;
        As(member.Id);
        var inPersonal = (await Send(new GetUrlsApproachingClickLimitQuery(0.8, 1, 10))).Value;

        // Assert
        inOrg.Items.Select(i => i.Id).Should().Equal(orgLink.Id);
        inPersonal.Items.Select(i => i.Id).Should().Equal(personal.Id);
    }
}
