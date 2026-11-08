using Microsoft.EntityFrameworkCore;
using Shortly.Core.ShortUrls.BulkActivateShortUrls;
using Shortly.Core.ShortUrls.BulkDeactivateShortUrls;
using Shortly.Core.ShortUrls.BulkDeleteShortUrls;
using Shortly.Core.ShortUrls.BulkUpdateExpiration;
using Shortly.Core.ShortUrls.DeactivateShortUrl;
using Shortly.Domain.Members;
using Shortly.Domain.ShortUrls;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.ShortUrls;

public class BulkShortUrlOperationsTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<ShortUrl> Stored(long id) => Query(db => db.Set<ShortUrl>().SingleAsync(s => s.Id == id));
 
    private Task<List<long>> ExistingIdsOf(IEnumerable<long> ids) =>
        Query(db => db.Set<ShortUrl>().Where(s => ids.Contains(s.Id)).Select(s => s.Id).ToListAsync());
 
    private Task<List<ShortUrl>> UserLinksAsync(Guid userId, int count)
    {
        return CreateManyAsync();
 
        async Task<List<ShortUrl>> CreateManyAsync()
        {
            var links = new List<ShortUrl>(count);
            for (var i = 0; i < count; i++) links.Add(await Data.UserLinkAsync(userId));
            return links;
        }
    }
 
    private async Task DeactivateAsAdmin(long shortUrlId)
    {
        As(Caller.Admin());
        await Send(new DeactivateShortUrlCommand(shortUrlId));
    }
 
    // ---- BulkActivateShortUrls ------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task BulkActivate_Should_ActivateEveryOwnedLink_AndReportFullSuccess()
    {
        // Arrange
        var user = await Data.UserAsync();
        var links = await UserLinksAsync(user.Id, 3);
        foreach (var link in links) await DeactivateAsAdmin(link.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new BulkActivateShortUrlsCommand(links.Select(l => l.Id).ToArray()));
 
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProcessed.Should().Be(3);
        result.Value.SuccessCount.Should().Be(3);
        result.Value.FailureCount.Should().Be(0);
        foreach (var link in links) (await Stored(link.Id)).IsActive.Should().BeTrue();
    }
 
    [Fact]
    public async Task BulkActivate_Should_SkipLinksTheCallerDoesNotOwn_AndStillSucceedOnTheRest()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var mine = (await UserLinksAsync(owner.Id, 2)).First();
        var stranger = await Data.UserAsync();
        var theirs = await Data.UserLinkAsync(stranger.Id);
        await DeactivateAsAdmin(mine.Id);
        await DeactivateAsAdmin(theirs.Id);
        As(owner.Id);
 
        // Act
        var result = await Send(new BulkActivateShortUrlsCommand([mine.Id, theirs.Id]));
 
        // Assert
        result.Value.TotalProcessed.Should().Be(2);
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(mine.Id)).IsActive.Should().BeTrue();
        (await Stored(theirs.Id)).IsActive.Should().BeFalse(); // untouched
    }
 
    [Fact]
    public async Task BulkActivate_Should_CountAnUnknownId_AsAFailure()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        await DeactivateAsAdmin(link.Id);
        As(user.Id);
 
        // Act
        var result = await Send(new BulkActivateShortUrlsCommand([link.Id, long.MaxValue]));
 
        // Assert
        result.Value.TotalProcessed.Should().Be(2);
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
    }
 
    [Fact]
    public async Task BulkActivate_Should_CountAnAlreadyActiveLink_AsAFailure()
    {
        // Arrange: Activate() itself returns AlreadyActive for a link that is already active
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id); // links start out active
        As(user.Id);
 
        // Act
        var result = await Send(new BulkActivateShortUrlsCommand([link.Id]));
 
        // Assert
        result.Value.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(1);
    }
 
    // ---- BulkDeactivateShortUrls ----------------------------------------------------------------------------------------
 
    [Fact]
    public async Task BulkDeactivate_Should_DeactivateEveryOwnedLink_ForAnOrganizationMember()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, member) = await Data.MemberAsync(org.Id, MemberRole.Member);
        var first = await Data.OrganizationLinkAsync(org.Id, member.Id);
        var second = await Data.OrganizationLinkAsync(org.Id, member.Id);
        As(user.Id, org.Id);
 
        // Act
        var result = await Send(new BulkDeactivateShortUrlsCommand([first.Id, second.Id]));
 
        // Assert
        result.Value.SuccessCount.Should().Be(2);
        (await Stored(first.Id)).IsActive.Should().BeFalse();
        (await Stored(second.Id)).IsActive.Should().BeFalse();
    }
 
    [Fact]
    public async Task BulkDeactivate_Should_RefuseEveryLink_ForAnOrganizationViewer()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (_, admin) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (viewerUser, _) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        var link = await Data.OrganizationLinkAsync(org.Id, admin.Id);
        As(viewerUser.Id, org.Id);
 
        // Act
        var result = await Send(new BulkDeactivateShortUrlsCommand([link.Id]));
 
        // Assert
        result.Value.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(link.Id)).IsActive.Should().BeTrue();
    }
 
    // ---- BulkDeleteShortUrls --------------------------------------------------------------------------------------------
 
    [Fact]
    public async Task BulkDelete_Should_RemoveEveryOwnedLink()
    {
        // Arrange
        var user = await Data.UserAsync();
        var links = await UserLinksAsync(user.Id, 3);
        As(user.Id);
 
        // Act
        var result = await Send(new BulkDeleteShortUrlsCommand(links.Select(l => l.Id).ToArray()));
 
        // Assert
        result.Value.SuccessCount.Should().Be(3);
        (await ExistingIdsOf(links.Select(l => l.Id))).Should().BeEmpty();
    }
 
    [Fact]
    public async Task BulkDelete_Should_LeaveUnownedLinksInPlace_AndReportThemAsFailures()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var mine = await Data.UserLinkAsync(owner.Id);
        var stranger = await Data.UserAsync();
        var theirs = await Data.UserLinkAsync(stranger.Id);
        As(owner.Id);
 
        // Act
        var result = await Send(new BulkDeleteShortUrlsCommand([mine.Id, theirs.Id]));
 
        // Assert
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        (await ExistingIdsOf([mine.Id])).Should().BeEmpty();
        (await ExistingIdsOf([theirs.Id])).Should().ContainSingle();
    }
 
    // ---- BulkUpdateExpiration ---------------------------------------------------------------------------------------
 
    [Fact]
    public async Task BulkUpdateExpiration_Should_SetTheSameExpiration_OnEveryOwnedLink()
    {
        // Arrange
        var user = await Data.UserAsync();
        var links = await UserLinksAsync(user.Id, 2);
        As(user.Id);
        var expiresAt = TestData.Now.AddDays(30);
 
        // Act
        var result = await Send(new BulkUpdateExpirationCommand(links.Select(l => l.Id).ToArray(), expiresAt));
 
        // Assert
        result.Value.SuccessCount.Should().Be(2);
        foreach (var link in links) (await Stored(link.Id)).ExpiresAt.Should().Be(expiresAt);
    }
 
    [Fact]
    public async Task BulkUpdateExpiration_Should_ClearExpiration_WhenGivenNull()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        await Send(new BulkUpdateExpirationCommand([link.Id], TestData.Now.AddDays(10)));
 
        // Act
        var result = await Send(new BulkUpdateExpirationCommand([link.Id], null));
 
        // Assert
        result.Value.SuccessCount.Should().Be(1);
        (await Stored(link.Id)).ExpiresAt.Should().BeNull();
    }
 
    [Fact]
    public async Task BulkUpdateExpiration_Should_CountAnUnownedLink_AsAFailure_WithoutTouchingIt()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var mine = await Data.UserLinkAsync(owner.Id);
        var stranger = await Data.UserAsync();
        var theirs = await Data.UserLinkAsync(stranger.Id);
        As(owner.Id);
        var expiresAt = TestData.Now.AddDays(15);
 
        // Act
        var result = await Send(new BulkUpdateExpirationCommand([mine.Id, theirs.Id], expiresAt));
 
        // Assert
        result.Value.TotalProcessed.Should().Be(2);
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(mine.Id)).ExpiresAt.Should().Be(expiresAt);
        (await Stored(theirs.Id)).ExpiresAt.Should().BeNull();
    }
 
    [Fact]
    public async Task BulkUpdateExpiration_Should_SaveTheSuccessfulItems_EvenWhenSomeFail()
    {
        // Arrange: a mix of an owned link and an unknown id in the same request
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);
        As(user.Id);
        var expiresAt = TestData.Now.AddDays(5);
 
        // Act
        var result = await Send(new BulkUpdateExpirationCommand([link.Id, long.MaxValue], expiresAt));
 
        // Assert
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(link.Id)).ExpiresAt.Should().Be(expiresAt);
    }
}
