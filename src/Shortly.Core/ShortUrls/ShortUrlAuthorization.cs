using Shortly.Core.Abstractions.Authentication;
using Shortly.Domain.Members;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls;

/// <summary>Centralized authorization policy helper for short URL management permissions.</summary>
internal static class ShortUrlAuthorization
{
    public static async Task<bool> CanManageAsync(
        ShortUrl shortUrl,
        IUserContext userContext,
        IOrganizationAuthorizationService organizationAuthorization,
        CancellationToken cancellationToken)
    {
        // System administrators bypass standard ownership checks
        if (userContext.IsAdmin) 
            return true;

        return shortUrl.OwnerType switch
        {
            enShortUrlOwnerType.User => 
                userContext.IsAuthenticated && shortUrl.UserId == userContext.UserId,

            enShortUrlOwnerType.Organization => 
                await CanManageOrganizationAsync(shortUrl, userContext, organizationAuthorization, cancellationToken),

            enShortUrlOwnerType.Anonymous => 
                !string.IsNullOrEmpty(userContext.AnonymousSessionId) && 
                shortUrl.AnonymousSessionId == userContext.AnonymousSessionId,

            _ => false
        };
    }

    private static async Task<bool> CanManageOrganizationAsync(
        ShortUrl shortUrl,
        IUserContext userContext,
        IOrganizationAuthorizationService organizationAuthorization,
        CancellationToken cancellationToken)
    {
        if (userContext.OrganizationId is null || shortUrl.OrganizationId != userContext.OrganizationId)
            return false;

        var membership = await organizationAuthorization.RequireMembershipAsync(
            shortUrl.OrganizationId.Value, cancellationToken);

        // Viewers are read-only members; only Members or higher are allowed to modify resources
        return membership.IsSuccess && membership.Value.Role >= MemberRole.Member;
    }
}