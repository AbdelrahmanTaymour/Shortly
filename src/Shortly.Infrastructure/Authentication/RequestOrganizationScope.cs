using Microsoft.AspNetCore.Http;

namespace Shortly.Infrastructure.Authentication;

/// <summary>
/// Manages the active request organization scope derived from URL routing parameters ({organizationId}), 
/// overriding the access token's organization claim for organization-scoped endpoints.
/// </summary>
public static class RequestOrganizationScope
{
    private const string Key = "Shortly.RequestOrganizationScope";

    /// <summary>Sets the verified organization context for the current HTTP request.</summary>
    public static void Set(HttpContext httpContext, Guid organizationId) => 
        httpContext.Items[Key] = organizationId;

    /// <summary>Retrieves the active organization context for the current HTTP request, if established.</summary>
    internal static Guid? Get(HttpContext? httpContext) =>
        httpContext?.Items.TryGetValue(Key, out var value) == true && value is Guid organizationId ? organizationId : null;
}