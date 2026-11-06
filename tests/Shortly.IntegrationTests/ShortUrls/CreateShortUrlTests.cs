using Microsoft.EntityFrameworkCore;
using Shortly.Core.ShortUrls.BulkCreateShortUrls;
using Shortly.Core.ShortUrls.CreateShortUrl;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;
using PlanEntity = Shortly.Domain.SubscriptionPlan.SubscriptionPlan;

namespace Shortly.IntegrationTests.ShortUrls;

public class CreateShortUrlTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static CreateShortUrlCommand Create(string url = "https://example.com/page", string? code = null) =>
        new(url, code, null, null, null, null, null, null);

    private Task<int> FreeLimit() => Query(db => db.Set<PlanEntity>().Where(p => p.Id == enSubscriptionPlan.Free).Select(p => p.MaxLinksPerMonth).SingleAsync());

    private Task SetUserMonthlyLinks(Guid userId, int count) => Seed(db =>
        db.Database.ExecuteSqlInterpolatedAsync($"UPDATE user_usage SET monthly_links_created = {count} WHERE user_id = {userId}"));

    private Task<ShortUrl> Stored(string code) => Query(db => db.Set<ShortUrl>().SingleAsync(s => s.ShortCode == ShortCode.Create(code).Value));

    private Task<UserUsage> UsageOf(Guid userId) => Query(db => db.Set<UserUsage>().SingleAsync(u => u.UserId == userId));

    // ---- single creation: owners ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_Should_StoreAPersonalLink_AndCountItInTheUsersQuota()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var result = await Send(Create());

        // Assert
        var link = await Stored(result.Value);
        link.OwnerType.Should().Be(enShortUrlOwnerType.User);
        link.UserId.Should().Be(user.Id);
        link.OriginalUrl.Value.Should().Be("https://example.com/page");
        link.ClickLimit.Should().Be(-1);
        link.TrackingEnabled.Should().BeTrue();
        var usage = await UsageOf(user.Id);
        usage.MonthlyLinksCreated.Should().Be(1);
        usage.TotalLinksCreated.Should().Be(1);
    }

    [Fact]
    public async Task Create_Should_StoreAnOrganizationLink_ForAMember_AndCountItInTheOrganizationsQuota()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, member) = await Data.MemberAsync(org.Id);
        As(user.Id, org.Id);

        // Act
        var result = await Send(Create());

        // Assert
        var link = await Stored(result.Value);
        link.OwnerType.Should().Be(enShortUrlOwnerType.Organization);
        link.OrganizationId.Should().Be(org.Id);
        link.CreatedByMemberId.Should().Be(member.Id);
        (await Query(db => db.Set<OrganizationUsage>().SingleAsync(u => u.OrganizationId == org.Id))).MonthlyLinksCreated.Should().Be(1);
        (await UsageOf(user.Id)).MonthlyLinksCreated.Should().Be(0);
    }

    [Fact]
    public async Task Create_Should_StoreAnAnonymousLink_ForAVisitorWithoutAnAccount()
    {
        // Arrange: no caller set, i.e. anonymous

        // Act
        var result = await Send(Create());

        // Assert
        var link = await Stored(result.Value);
        link.OwnerType.Should().Be(enShortUrlOwnerType.Anonymous);
        link.AnonymousSessionId.Should().Be(Caller.Anonymous.AnonymousSessionId);
        link.UserId.Should().BeNull();
    }

    [Fact]
    public async Task Create_Should_RefuseAViewer_AndAnInactiveOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (viewer, _) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        var inactive = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(inactive.Id);
        await Seed(async db => (await db.Set<Organization>().SingleAsync(o => o.Id == inactive.Id)).Deactivate(TestData.Now));

        // Act
        As(viewer.Id, org.Id);
        var asViewer = await Send(Create());
        As(member.Id, inactive.Id);
        var inInactive = await Send(Create());

        // Assert
        asViewer.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        inInactive.IsFailure.Should().BeTrue();
        (await Query(db => db.Set<ShortUrl>().CountAsync(s => s.OrganizationId == org.Id || s.OrganizationId == inactive.Id))).Should().Be(0);
    }

    [Fact]
    public async Task Create_Should_ReturnNotFound_ForACallerWhoDoesNotExist()
    {
        // Arrange
        As(Guid.NewGuid());

        // Act
        var result = await Send(Create());

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }

    // ---- options and rules ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_Should_ApplyTheRequestedOptions()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
        var expires = TestData.Now.AddDays(3);

        // Act
        var result = await Send(new CreateShortUrlCommand("https://example.com/opts", null, 5, false, true, expires, "My title", "About it"));

        // Assert
        var link = await Stored(result.Value);
        link.ClickLimit.Should().Be(5);
        link.TrackingEnabled.Should().BeFalse();
        link.IsPrivate.Should().BeTrue();
        link.ExpiresAt.Should().BeCloseTo(expires, TimeSpan.FromSeconds(1));
        link.Title.Should().Be("My title");
        link.Description.Should().Be("About it");
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("")]
    public async Task Create_Should_RejectAnInvalidUrl_AndStoreNothing(string url)
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var act = () => Send(Create(url));

        // Assert: the validation behavior refuses it before anything is stored
        await act.Should().ThrowAsync<Shortly.Core.Exceptions.ValidationException>();
        (await Query(db => db.Set<ShortUrl>().CountAsync(s => s.UserId == user.Id))).Should().Be(0);
        (await UsageOf(user.Id)).MonthlyLinksCreated.Should().Be(0);
    }

    [Fact]
    public async Task Create_Should_RefuseACustomCode_OnTheFreePlan_ButAllowItOnStarter()
    {
        // Arrange
        var free = await Data.UserAsync();
        var starter = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var code = "c" + Guid.NewGuid().ToString("N")[..8];

        // Act
        As(free.Id);
        var onFree = await Send(Create(code: code));
        As(starter.Id);
        var onStarter = await Send(Create(code: code));

        // Assert
        onFree.Error.Should().Be(ShortUrlErrors.CustomShortCodeNotAllowed);
        onStarter.Value.Should().Be(code);
    }

    [Fact]
    public async Task Create_Should_ReturnCodeAlreadyExists_ForATakenCustomCode_AndNotCountIt()
    {
        // Arrange
        var first = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var second = await Data.UserAsync(plan: enSubscriptionPlan.Starter);
        var code = "c" + Guid.NewGuid().ToString("N")[..8];
        As(first.Id);
        await Send(Create(code: code));

        // Act
        As(second.Id);
        var result = await Send(Create(code: code));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.ShortCodeAlreadyExists);
        (await UsageOf(second.Id)).MonthlyLinksCreated.Should().Be(0);
    }

    [Fact]
    public async Task Create_Should_GenerateADifferentCode_ForEachLink()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        As(user.Id);

        // Act
        var codes = new List<string>();
        for (var i = 0; i < 5; i++) codes.Add((await Send(Create())).Value);

        // Assert
        codes.Should().OnlyHaveUniqueItems();
    }

    // ---- quota ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_Should_RefuseTheLinkBeyondTheMonthlyQuota_AndAllowTheLastOne()
    {
        // Arrange
        var user = await Data.UserAsync();
        var limit = await FreeLimit();
        await SetUserMonthlyLinks(user.Id, limit - 1);
        As(user.Id);

        // Act
        var last = await Send(Create());
        var beyond = await Send(Create());

        // Assert
        last.IsSuccess.Should().BeTrue();
        beyond.Error.Should().Be(ShortUrlErrors.MonthlyLinkLimitReached);
        (await UsageOf(user.Id)).MonthlyLinksCreated.Should().Be(limit);
        (await Query(db => db.Set<ShortUrl>().CountAsync(s => s.UserId == user.Id))).Should().Be(1);
    }

    [Fact]
    public async Task Create_Should_NeverExceedTheQuota_WhenRequestsArriveTogether()
    {
        // Arrange: exactly one link left
        var user = await Data.UserAsync();
        var limit = await FreeLimit();
        await SetUserMonthlyLinks(user.Id, limit - 1);
        As(user.Id);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Send(Create())));

        // Assert
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Where(r => r.IsFailure).Select(r => r.Error).Should().OnlyContain(e =>
            e == ShortUrlErrors.MonthlyLinkLimitReached || e == ShortUrlErrors.ConcurrentUpdate);
        (await UsageOf(user.Id)).MonthlyLinksCreated.Should().Be(limit);
    }

    // ---- bulk creation -------------------------------------------------------------------------------------------------

    private static BulkCreateShortUrlsCommand Bulk(params string[] urls) =>
        new(urls.Select(u => new BulkCreateShortUrlItem(u, null, null)).ToList());

    [Fact]
    public async Task Bulk_Should_RefuseAPlanWithoutBulkCreation_AndAnAnonymousCaller()
    {
        // Arrange
        var free = await Data.UserAsync();

        // Act
        As(free.Id);
        var onFree = await Send(Bulk("https://example.com/a"));
        As(Caller.Anonymous);
        var anonymous = await Send(Bulk("https://example.com/a"));

        // Assert
        onFree.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
        anonymous.Error.Should().Be(ShortUrlErrors.BulkCreationNotAllowed);
    }

    [Fact]
    public async Task Bulk_Should_CreateEveryLink_AndCountThemInTheQuota()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        As(user.Id);

        // Act
        var result = await Send(Bulk("https://example.com/1", "https://example.com/2", "https://example.com/3"));

        // Assert
        result.Value.TotalProcessed.Should().Be(3);
        result.Value.SuccessCount.Should().Be(3);
        result.Value.FailureCount.Should().Be(0);
        var stored = await Query(db => db.Set<ShortUrl>().Where(s => result.Value.CreatedIds.Contains(s.Id)).ToListAsync());
        stored.Should().HaveCount(3).And.OnlyContain(s => s.UserId == user.Id);
        stored.Select(s => s.ShortCode.Value).Should().OnlyHaveUniqueItems();
        (await UsageOf(user.Id)).MonthlyLinksCreated.Should().Be(3);
    }

    [Fact]
    public async Task Bulk_Should_RejectTheWholeRequest_WhenAnItemIsNotAValidUrl()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        As(user.Id);

        // Act
        var act = () => Send(Bulk("https://example.com/1", "not a url"));

        // Assert
        await act.Should().ThrowAsync<Shortly.Core.Exceptions.ValidationException>();
        (await Query(db => db.Set<ShortUrl>().CountAsync(s => s.UserId == user.Id))).Should().Be(0);
    }

    [Fact]
    public async Task Bulk_Should_CreateNothing_WhenTheValidLinksDoNotFitTheRemainingQuota()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Professional);
        await SetUserMonthlyLinks(user.Id, 499); // Professional allows 500 a month
        As(user.Id);

        // Act
        var result = await Send(Bulk("https://example.com/1", "https://example.com/2"));

        // Assert
        result.Error.Should().Be(ShortUrlErrors.MonthlyLinkLimitReached);
        (await Query(db => db.Set<ShortUrl>().CountAsync(s => s.UserId == user.Id))).Should().Be(0);
        (await UsageOf(user.Id)).MonthlyLinksCreated.Should().Be(499);
    }

    [Fact]
    public async Task Bulk_Should_CreateOrganizationLinks_ForAMemberOfAnOrganizationOnABulkPlan()
    {
        // Arrange
        var org = await Data.OrganizationAsync(plan: enSubscriptionPlan.Professional);
        var (user, member) = await Data.MemberAsync(org.Id);
        As(user.Id, org.Id);

        // Act
        var result = await Send(Bulk("https://example.com/1", "https://example.com/2"));

        // Assert
        var stored = await Query(db => db.Set<ShortUrl>().Where(s => s.OrganizationId == org.Id).ToListAsync());
        stored.Should().HaveCount(2).And.OnlyContain(s => s.CreatedByMemberId == member.Id);
        (await Query(db => db.Set<OrganizationUsage>().SingleAsync(u => u.OrganizationId == org.Id))).MonthlyLinksCreated.Should().Be(2);
    }
}
