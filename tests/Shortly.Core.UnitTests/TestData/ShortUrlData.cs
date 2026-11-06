using Shortly.Domain.ShortUrls;

namespace Shortly.Core.UnitTests.TestData;

internal static class ShortUrlData
{
    public static ShortUrl OrganizationLink(Guid organizationId, Guid? createdByMemberId = null, string code = "abc1234") =>
        ShortUrl.CreateForOrganization(
            organizationId, createdByMemberId ?? Guid.NewGuid(),
            OriginalUrl.Create("https://example.com").Value, ShortCode.Create(code).Value, TestClock.UtcNow).Value;

    /// <summary>Database-generated ids do not exist in memory, so tests that look links up by id assign one.</summary>
    public static ShortUrl WithId(this ShortUrl link, long id)
    {
        typeof(Shortly.Domain.Abstractions.Entity<long>).GetProperty(nameof(ShortUrl.Id))!.SetValue(link, id);
        return link;
    }

    public static ShortUrl UserLink(Guid ownerId, string code = "abc1234") =>
        ShortUrl.CreateForUser(
            ownerId, OriginalUrl.Create("https://example.com").Value, ShortCode.Create(code).Value, TestClock.UtcNow).Value;
}
