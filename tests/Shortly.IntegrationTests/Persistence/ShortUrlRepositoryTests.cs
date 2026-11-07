using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Domain.ClickEvents;
using Shortly.Domain.ShortUrls;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Persistence;

public class ShortUrlRepositoryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static ShortCode Code(string value) => ShortCode.Create(value).Value;

    private static string Unique(string prefix = "t") => prefix + Guid.NewGuid().ToString("N")[..7];

    // ---- lookups ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetByShortCodeAsync_Should_FindALink_ByItsCode()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);

        // Act
        var found = await WithRepository<IShortUrlRepository, ShortUrl?>(r => r.GetByShortCodeAsync(link.ShortCode), save: false);
        var missing = await WithRepository<IShortUrlRepository, ShortUrl?>(r => r.GetByShortCodeAsync(Code(Unique("m"))), save: false);

        // Assert
        found!.Id.Should().Be(link.Id);
        missing.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdsAsync_Should_ReturnTheExistingLinks_AndNothingForAnEmptyList()
    {
        // Arrange
        var user = await Data.UserAsync();
        var a = await Data.UserLinkAsync(user.Id);
        var b = await Data.UserLinkAsync(user.Id);

        // Act
        var found = await WithRepository<IShortUrlRepository, IReadOnlyList<ShortUrl>>(r => r.GetByIdsAsync([a.Id, b.Id, -5]), save: false);
        var none = await WithRepository<IShortUrlRepository, IReadOnlyList<ShortUrl>>(r => r.GetByIdsAsync([]), save: false);

        // Assert
        found.Select(l => l.Id).Should().BeEquivalentTo([a.Id, b.Id]);
        none.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByShortCodeAsync_Should_ReportWhetherTheCodeIsTaken()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);

        // Act / Assert
        (await WithRepository<IShortUrlRepository, bool>(r => r.ExistsByShortCodeAsync(link.ShortCode), save: false)).Should().BeTrue();
        (await WithRepository<IShortUrlRepository, bool>(r => r.ExistsByShortCodeAsync(Code(Unique("f"))), save: false)).Should().BeFalse();
    }

    [Fact]
    public async Task GetExistingShortCodesAsync_Should_ReturnOnlyTheTakenCodes()
    {
        // Arrange
        var user = await Data.UserAsync();
        var taken = await Data.UserLinkAsync(user.Id);
        var free = Code(Unique("f"));

        // Act
        var existing = await WithRepository<IShortUrlRepository, HashSet<string>>(
            r => r.GetExistingShortCodesAsync([taken.ShortCode, free]), save: false);
        var none = await WithRepository<IShortUrlRepository, HashSet<string>>(r => r.GetExistingShortCodesAsync([]), save: false);

        // Assert
        existing.Should().BeEquivalentTo([taken.ShortCode.Value]);
        none.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsAnyForUserAsync_Should_BeTrueOnlyForAUserWithLinks()
    {
        // Arrange
        var withLinks = await Data.UserAsync();
        await Data.UserLinkAsync(withLinks.Id);
        var without = await Data.UserAsync();

        // Act / Assert
        (await WithRepository<IShortUrlRepository, bool>(r => r.ExistsAnyForUserAsync(withLinks.Id), save: false)).Should().BeTrue();
        (await WithRepository<IShortUrlRepository, bool>(r => r.ExistsAnyForUserAsync(without.Id), save: false)).Should().BeFalse();
    }

    // ---- bulk operations -------------------------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAllForUserAsync_Should_DeleteTheUsersLinks_AndTheirClicks_ButNotOthers()
    {
        // Arrange
        var user = await Data.UserAsync();
        var other = await Data.UserAsync();
        var mine = await Data.UserLinkAsync(user.Id);
        await Data.ClickAsync(mine.Id);
        var theirs = await Data.UserLinkAsync(other.Id);

        // Act
        var deleted = await WithRepository<IShortUrlRepository, int>(r => r.DeleteAllForUserAsync(user.Id), save: false);

        // Assert
        deleted.Should().Be(1);
        (await Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == mine.Id))).Should().BeFalse();
        (await Query(db => db.Set<ClickEvent>().AnyAsync(c => c.ShortUrlId == mine.Id))).Should().BeFalse();
        (await Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == theirs.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAllForOrganizationAsync_Should_DeleteTheOrganizationsLinks_AndTheirClicks_ButNotOthers()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var otherOrg = await Data.OrganizationAsync();
        var orgLink = await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id);
        await Data.ClickAsync(orgLink.Id);
        var otherLink = await Data.OrganizationLinkAsync(otherOrg.Id, otherOrg.OwnerMember.Id);
        var personal = await Data.UserLinkAsync(org.Owner.Id);

        // Act
        var deleted = await WithRepository<IShortUrlRepository, int>(r => r.DeleteAllForOrganizationAsync(org.Id), save: false);

        // Assert
        deleted.Should().Be(1);
        (await Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == orgLink.Id))).Should().BeFalse();
        (await Query(db => db.Set<ClickEvent>().AnyAsync(c => c.ShortUrlId == orgLink.Id))).Should().BeFalse();
        (await Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == otherLink.Id))).Should().BeTrue();
        (await Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == personal.Id))).Should().BeTrue();
    }

    // ---- constraints -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Saving_Should_Fail_ForADuplicateShortCode()
    {
        // Arrange
        var user = await Data.UserAsync();
        var link = await Data.UserLinkAsync(user.Id);

        // Act
        var duplicate = () => Data.UserLinkAsync(user.Id, code: link.ShortCode.Value);

        // Assert
        await duplicate.Should().ThrowAsync<UniqueConstraintViolationException>();
    }

    [Fact]
    public async Task ShortCodes_Should_BeCaseSensitive_SoTwoCodesDifferingOnlyInCaseCoexist()
    {
        // Arrange
        var user = await Data.UserAsync();
        var lower = Unique("a");
        await Data.UserLinkAsync(user.Id, code: lower);

        // Act
        var upper = () => Data.UserLinkAsync(user.Id, code: lower.ToUpperInvariant());

        // Assert: /abc and /ABC are different links, and availability checks are exact
        await upper.Should().NotThrowAsync();
        (await WithRepository<IShortUrlRepository, bool>(r => r.ExistsByShortCodeAsync(Code(lower)), save: false)).Should().BeTrue();
    }

    [Fact]
    public async Task DeletingAnOrganization_Should_CascadeToItsLinks_AtTheDatabaseLevel()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var link = await Data.OrganizationLinkAsync(org.Id, org.OwnerMember.Id);

        // Act
        await Seed(async db => db.Remove(await db.Set<Shortly.Domain.Organizations.Organization>().SingleAsync(o => o.Id == org.Id)));

        // Assert
        (await Query(db => db.Set<ShortUrl>().AnyAsync(s => s.Id == link.Id))).Should().BeFalse();
    }
}
