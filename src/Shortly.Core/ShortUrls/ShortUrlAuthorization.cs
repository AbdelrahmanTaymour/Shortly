using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Enums;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls;

internal static class ShortUrlAuthorization
{
    public static bool CanManage(ShortUrl shortUrl, IUserContext userContext)
    {
        if (userContext.IsAdmin) return true;

        return shortUrl.OwnerType switch
        {
            enShortUrlOwnerType.User => userContext.IsAuthenticated && shortUrl.UserId == userContext.UserId,
            enShortUrlOwnerType.Organization => userContext.OrganizationId is not null &&
                                                shortUrl.OrganizationId == userContext.OrganizationId,
            enShortUrlOwnerType.Anonymous => !string.IsNullOrEmpty(userContext.AnonymousSessionId) &&
                                             shortUrl.AnonymousSessionId == userContext.AnonymousSessionId,
            _ => false
        };
    }
}