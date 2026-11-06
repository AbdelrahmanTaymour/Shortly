using Microsoft.EntityFrameworkCore;
using Shortly.Core.ShortUrls.SearchShortUrls;
using Shortly.Domain.ShortUrls;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.ShortUrls;

public class SearchShortUrlsTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static SearchShortUrlsQuery Query(
        string? search = null, ShortUrlStatusFilter status = ShortUrlStatusFilter.All,
        ShortUrlVisibilityFilter visibility = ShortUrlVisibilityFilter.All, DateTime? from = null, DateTime? to = null,
        ShortUrlSort sort = ShortUrlSort.Newest, int page = 1, int pageSize = 20) =>
        new(search, status, visibility, from, to, sort, page, pageSize);

    private Task SetTitle(long id, string title) => Seed(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE short_urls SET title = {title} WHERE id = {id}"));

    private Task MakePrivate(long id) => Seed(async db =>
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE short_urls SET is_private = TRUE WHERE id = {id}"));

    private Task Change(long id, Action<ShortUrl> change) => Seed(async db => change(await db.Set<ShortUrl>().SingleAsync(s => s.Id == id)));

    // ---- scope ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Search_Should_ReturnOnlyTheCallersOwnLinks()
    {
        // Arrange
        var user = await Data.UserAsync();
        var other = await Data.UserAsync();
        var mine = await Data.UserLinkAsync(user.Id);
        await Data.UserLinkAsync(other.Id);
        As(user.Id);

        // Act
        var result = await Send(Query());

        // Assert
        result.Value.Items.Select(i => i.Id).Should().Equal(mine.Id);
    }

    [Fact]
    public async Task Search_Should_ReturnTheOrganizationLinks_WhenWorkingInAnOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, member) = await Data.MemberAsync(org.Id);
        var orgLink = await Data.OrganizationLinkAsync(org.Id, member.Id);
        await Data.UserLinkAsync(user.Id);
        As(user.Id, org.Id);

        // Act
        var result = await Send(Query());

        // Assert
        result.Value.Items.Select(i => i.Id).Should().Equal(orgLink.Id);
    }

    // ---- filters -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Search_Should_FilterByStatus_ForAPersonalWorkspace()
    {
        // Arrange
        var user = await Data.UserAsync();
        var active = await Data.UserLinkAsync(user.Id);
        var inactive = await Data.UserLinkAsync(user.Id);
        await Change(inactive.Id, l => l.Deactivate(TestData.Now));
        As(user.Id);

        // Act
        var onlyActive = await Send(Query(status: ShortUrlStatusFilter.Active));
        var onlyInactive = await Send(Query(status: ShortUrlStatusFilter.Inactive));

        // Assert
        onlyActive.Value.Items.Select(i => i.Id).Should().Equal(active.Id);
        onlyInactive.Value.Items.Select(i => i.Id).Should().Equal(inactive.Id);
    }

    [Fact]
    public async Task Search_Should_FilterByText_InTheTitleAndTheOriginalUrl_IgnoringCase()
    {
        // Arrange
        var user = await Data.UserAsync();
        var titled = await Data.UserLinkAsync(user.Id);
        await SetTitle(titled.Id, "Quarterly REPORT");
        await Data.UserLinkAsync(user.Id); // url is https://example.com/integration-test
        As(user.Id);

        // Act
        var byTitle = await Send(Query(search: "report"));
        var byUrl = await Send(Query(search: "INTEGRATION-test"));
        var nothing = await Send(Query(search: "no-such-text"));

        // Assert
        byTitle.Value.Items.Select(i => i.Id).Should().Equal(titled.Id);
        byUrl.Value.TotalCount.Should().Be(2);
        nothing.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_Should_FilterByCreationDate_InclusiveOfBothEnds()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);

        // Act
        var within = await Send(Query(from: link.CreatedAtUtc, to: link.CreatedAtUtc));
        var before = await Send(Query(to: link.CreatedAtUtc.AddMinutes(-1)));
        var after = await Send(Query(from: link.CreatedAtUtc.AddMinutes(1)));

        // Assert
        within.Value.Items.Should().HaveCount(1);
        before.Value.Items.Should().BeEmpty();
        after.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_Should_FilterByVisibility()
    {
        // Arrange
        var user = await Data.UserAsync();
        var publicLink = await Data.UserLinkAsync(user.Id);
        var privateLink = await Data.UserLinkAsync(user.Id);
        await MakePrivate(privateLink.Id);
        As(user.Id);

        // Act
        var onlyPublic = await Send(Query(visibility: ShortUrlVisibilityFilter.Public));
        var onlyPrivate = await Send(Query(visibility: ShortUrlVisibilityFilter.Private));

        // Assert
        onlyPublic.Value.Items.Select(i => i.Id).Should().Equal(publicLink.Id);
        onlyPrivate.Value.Items.Select(i => i.Id).Should().Equal(privateLink.Id);
    }

    [Fact]
    public async Task Search_Should_CombineEveryFilter_WithAnd()
    {
        // Arrange
        var user = await Data.UserAsync();
        var match = await Data.UserLinkAsync(user.Id);
        await SetTitle(match.Id, "Launch plan");
        var inactiveMatch = await Data.UserLinkAsync(user.Id);
        await SetTitle(inactiveMatch.Id, "Launch plan");
        await Change(inactiveMatch.Id, l => l.Deactivate(TestData.Now));
        As(user.Id);

        // Act
        var result = await Send(Query(search: "launch", status: ShortUrlStatusFilter.Active));

        // Assert
        result.Value.Items.Select(i => i.Id).Should().Equal(match.Id);
    }

    // ---- sorting and paging --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Search_Should_SortNewestOldestAndMostClicked()
    {
        // Arrange
        var user = await Data.UserAsync();
        var a = await Data.UserLinkAsync(user.Id);
        await Task.Delay(20);
        var b = await Data.UserLinkAsync(user.Id);
        await Task.Delay(20);
        var c = await Data.UserLinkAsync(user.Id);
        await Seed(async db => await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE short_urls SET total_clicks = 9 WHERE id = {a.Id}"));
        As(user.Id);

        // Act
        var newest = await Send(Query(sort: ShortUrlSort.Newest));
        var oldest = await Send(Query(sort: ShortUrlSort.Oldest));
        var mostClicked = await Send(Query(sort: ShortUrlSort.MostClicked));

        // Assert
        newest.Value.Items.Select(i => i.Id).Should().Equal(c.Id, b.Id, a.Id);
        oldest.Value.Items.Select(i => i.Id).Should().Equal(a.Id, b.Id, c.Id);
        mostClicked.Value.Items.First().Id.Should().Be(a.Id);
    }

    [Fact]
    public async Task Search_Should_PageTheResults()
    {
        // Arrange
        var user = await Data.UserAsync();
        for (var i = 0; i < 5; i++) { await Data.UserLinkAsync(user.Id); await Task.Delay(5); }
        As(user.Id);

        // Act
        var page2 = await Send(Query(page: 2, pageSize: 2));
        var clamped = await Send(Query(page: 0, pageSize: 500));

        // Assert
        page2.Value.Items.Should().HaveCount(2);
        page2.Value.TotalCount.Should().Be(5);
        page2.Value.TotalPages.Should().Be(3);
        clamped.Value.Page.Should().Be(1);
        clamped.Value.PageSize.Should().Be(50);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    public async Task Search_Should_TreatWildcardCharactersInTheSearchTextLiterally(string text)
    {
        // Arrange
        var user = await Data.UserAsync();
        var plain = await Data.UserLinkAsync(user.Id);
        await SetTitle(plain.Id, "Sales report");
        As(user.Id);

        // Act
        var result = await Send(Query(search: text));

        // Assert: a literal "%" matches nothing here instead of every link
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_Should_StillFindATitleThatContainsTheWildcardCharacters()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        await SetTitle(link.Id, "100%_off");
        await SetTitle((await Data.UserLinkAsync(user.Id)).Id, "1000 offers");
        As(user.Id);

        // Act
        var result = await Send(Query(search: "100%_off"));

        // Assert
        result.Value.Items.Select(i => i.Id).Should().Equal(link.Id);
    }
}
