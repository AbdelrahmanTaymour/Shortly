using Shortly.Core.ShortUrls.GetMyShortUrls;
using Shortly.Core.ShortUrls.GetShortUrlByCode;
using Shortly.Core.ShortUrls.GetShortUrlById;
using Shortly.Domain.ShortUrls;
using Shortly.IntegrationTests.Infrastructure;
using Shortly.Domain.Members;

namespace Shortly.IntegrationTests.ShortUrls;

public class ShortUrlReadTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    // ---- my links ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetMyShortUrls_Should_ListOnlyThePersonalLinksOfTheCaller_NewestFirst()
    {
        // Arrange
        var user = await Data.UserAsync();
        var other = await Data.UserAsync();
        var first = await Data.UserLinkAsync(user.Id);
        await Task.Delay(20);
        var second = await Data.UserLinkAsync(user.Id);
        await Data.UserLinkAsync(other.Id);
        As(user.Id);

        // Act
        var result = await Send(new GetMyShortUrlsQuery());

        // Assert
        result.Value.Items.Select(i => i.Id).Should().Equal(second.Id, first.Id);
        result.Value.TotalCount.Should().Be(2);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetMyShortUrls_Should_PageTheResults_AndReportTheTotals()
    {
        // Arrange
        var user = await Data.UserAsync();
        for (var i = 0; i < 5; i++) { await Data.UserLinkAsync(user.Id); await Task.Delay(5); }
        As(user.Id);

        // Act
        var page1 = await Send(new GetMyShortUrlsQuery(Page: 1, PageSize: 2));
        var page3 = await Send(new GetMyShortUrlsQuery(Page: 3, PageSize: 2));
        var beyond = await Send(new GetMyShortUrlsQuery(Page: 4, PageSize: 2));

        // Assert
        page1.Value.Items.Should().HaveCount(2);
        page1.Value.TotalCount.Should().Be(5);
        page1.Value.TotalPages.Should().Be(3);
        page3.Value.Items.Should().HaveCount(1);
        beyond.Value.Items.Should().BeEmpty();
        beyond.Value.TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-3, 20, 1, 20)]
    [InlineData(1, 500, 1, 50)]
    [InlineData(1, 0, 1, 10)]
    public async Task GetMyShortUrls_Should_ClampOutOfRangePaging(int page, int pageSize, int expectedPage, int expectedSize)
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var result = await Send(new GetMyShortUrlsQuery(page, pageSize));

        // Assert
        result.Value.Page.Should().Be(expectedPage);
        result.Value.PageSize.Should().Be(expectedSize);
    }

    [Fact]
    public async Task GetMyShortUrls_Should_ListTheOrganizationLinks_WhenTheCallerWorksInAnOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, member) = await Data.MemberAsync(org.Id);
        var orgLink = await Data.OrganizationLinkAsync(org.Id, member.Id);
        var personal = await Data.UserLinkAsync(user.Id);
        As(user.Id, org.Id);

        // Act
        var inOrg = await Send(new GetMyShortUrlsQuery());
        As(user.Id);
        var personally = await Send(new GetMyShortUrlsQuery());

        // Assert
        inOrg.Value.Items.Select(i => i.Id).Should().Equal(orgLink.Id);
        personally.Value.Items.Select(i => i.Id).Should().Equal(personal.Id);
    }

    [Fact]
    public async Task GetMyShortUrls_Should_RefuseAStaleOrganizationClaim_WhenTheMemberWasRemoved()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, _) = await Data.MemberAsync(org.Id, isActive: false);
        As(user.Id, org.Id);

        // Act
        var result = await Send(new GetMyShortUrlsQuery());

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyShortUrls_Should_MapEveryField()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id, clickLimit: 25);
        As(user.Id);

        // Act
        var item = (await Send(new GetMyShortUrlsQuery())).Value.Items.Single();

        // Assert
        item.Id.Should().Be(link.Id);
        item.ShortCode.Should().Be(link.ShortCode.Value);
        item.OriginalUrl.Should().Be(link.OriginalUrl.Value);
        item.ClickLimit.Should().Be(25);
        item.TotalClicks.Should().Be(0);
        item.IsActive.Should().BeTrue();
        item.IsPrivate.Should().BeFalse();
        item.IsPasswordProtected.Should().BeFalse();
        item.CreatedAtUtc.Should().BeCloseTo(link.CreatedAtUtc, TimeSpan.FromSeconds(1));
    }

    // ---- by id / by code -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetById_And_GetByCode_Should_ReturnAPersonalLink_ToItsOwner()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);

        // Act
        var byId = await Send(new GetShortUrlByIdQuery(link.Id));
        var byCode = await Send(new GetShortUrlByCodeQuery(link.ShortCode.Value));

        // Assert
        byId.Value.ShortCode.Should().Be(link.ShortCode.Value);
        byCode.Value.Id.Should().Be(link.Id);
    }

    [Fact]
    public async Task GetById_And_GetByCode_Should_ReturnNotFound_ForSomeoneElsesLink()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var stranger = await Data.UserAsync();
        var link = await Data.UserLinkAsync(owner.Id);
        As(stranger.Id);

        // Act
        var byId = await Send(new GetShortUrlByIdQuery(link.Id));
        var byCode = await Send(new GetShortUrlByCodeQuery(link.ShortCode.Value));

        // Assert: NotFound, not Forbidden, so a link's existence is not revealed
        byId.Error.Should().Be(ShortUrlErrors.NotFound);
        byCode.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task GetById_Should_ReturnAnOrganizationLink_ToItsMembers_ButNotToOutsiders()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, memberRow) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        var outsider = await Data.UserAsync();
        var link = await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id);

        // Act
        As(member.Id, org.Id);
        var asMember = await Send(new GetShortUrlByIdQuery(link.Id));
        As(outsider.Id);
        var asOutsider = await Send(new GetShortUrlByIdQuery(link.Id));
        As(member.Id); // the same member, but in the personal workspace
        var inPersonal = await Send(new GetShortUrlByIdQuery(link.Id));

        // Assert
        asMember.IsSuccess.Should().BeTrue();
        asOutsider.Error.Should().Be(ShortUrlErrors.NotFound);
        inPersonal.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task GetByCode_Should_ReturnAnOrganizationLink_ToItsMembers_ButNotToOutsiders()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);
        var outsider = await Data.UserAsync();
        var otherOrg = await Data.OrganizationAsync();
        var link = await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id);

        // Act
        As(member.Id, org.Id);
        var asMember = await Send(new GetShortUrlByCodeQuery(link.ShortCode.Value));
        As(outsider.Id);
        var asOutsider = await Send(new GetShortUrlByCodeQuery(link.ShortCode.Value));
        As(otherOrg.Owner.Id, otherOrg.Id);
        var fromOtherOrg = await Send(new GetShortUrlByCodeQuery(link.ShortCode.Value));

        // Assert
        asMember.IsSuccess.Should().BeTrue();
        asOutsider.Error.Should().Be(ShortUrlErrors.NotFound);
        fromOtherOrg.Error.Should().Be(ShortUrlErrors.NotFound);
    }

    [Fact]
    public async Task GetById_And_GetByCode_Should_ReturnAnyLink_ToAnAdministrator()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var link = await Data.UserLinkAsync(owner.Id);
        As(Caller.Admin());

        // Act
        var byId = await Send(new GetShortUrlByIdQuery(link.Id));
        var byCode = await Send(new GetShortUrlByCodeQuery(link.ShortCode.Value));

        // Assert
        byId.IsSuccess.Should().BeTrue();
        byCode.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_And_GetByCode_Should_ReturnNotFound_ForUnknownValues()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act / Assert
        (await Send(new GetShortUrlByIdQuery(long.MaxValue))).Error.Should().Be(ShortUrlErrors.NotFound);
        (await Send(new GetShortUrlByCodeQuery("zz-unknown"))).Error.Should().Be(ShortUrlErrors.NotFound);
    }
}
