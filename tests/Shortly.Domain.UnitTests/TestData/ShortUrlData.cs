using Shortly.Domain.ShortUrls;

namespace Shortly.Domain.UnitTests.TestData;

internal static class ShortUrlData
{
    public static OriginalUrl Url(string value = "https://example.com/page") => OriginalUrl.Create(value).Value;

    public static ShortCode Code(string value = "abc1234") => ShortCode.Create(value).Value;

    public static ShortUrl CreateUserLink(
        Guid? userId = null,
        int clickLimit = -1,
        DateTime? expiresAt = null,
        string code = "abc1234") =>
        ShortUrl.CreateForUser(userId ?? Guid.NewGuid(), Url(), Code(code), TestClock.Now,
            clickLimit, expiresAt: expiresAt).Value;

    public static ShortUrl CreateOrganizationLink(Guid? organizationId = null, Guid? memberId = null) =>
        ShortUrl.CreateForOrganization(organizationId ?? Guid.NewGuid(), memberId ?? Guid.NewGuid(),
            Url(), Code(), TestClock.Now).Value;
}
