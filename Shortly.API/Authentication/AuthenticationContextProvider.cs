using System.Net;
using System.Security.Claims;
using Shortly.Core.Models;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Domain.Enums;

namespace Shortly.API.Authentication;

/// <summary>
/// Provides authentication context information for the current HTTP request,
/// including user authentication status, IP address, and owner type determination.
/// Handles both authenticated users and anonymous sessions with persistent identification.
/// </summary>
public class AuthenticationContextProvider : IAuthenticationContextProvider
{
    /// <summary>
    /// Claim type for user identifier from the standard claims.
    /// </summary>
    private const string ClaimUserId = ClaimTypes.NameIdentifier;
    
    /// <summary>
    /// Custom claim type for organization member identifier.
    /// </summary>
    private const string ClaimMemberId = "MemberId";
    
    /// <summary>
    /// Custom claim type for organization identifier.
    /// </summary>
    private const string ClaimOrganizationId = "OrganizationId";

    
    /// <inheritdoc />
    public IAuthenticationContext GetCurrentContextAsync(HttpContext httpContext)
    {
       var context = new AuthenticationContext
        {
            IpAddress = GetClientIpAddress(httpContext)
        };
       
       if(!IsAuthenticated(httpContext))
           return CreateAnonymousContext(context, httpContext);
       
       PopulateAuthenticatedContext(context, httpContext);
       
        return context;
    }
    
    
    /// <inheritdoc />
    public bool IsAuthenticated(HttpContext httpContext) 
        => httpContext.User.Identity?.IsAuthenticated == true;
    
    
    /// <inheritdoc/>>
    public string GetAnonymousId(HttpContext httpContext)
    {
        const string cookieName = "AnonId";
        if (httpContext.Request.Cookies.TryGetValue(cookieName, out var anonId) && !string.IsNullOrWhiteSpace(anonId))
        {
            return anonId;
        }
        
        anonId = Guid.NewGuid().ToString();
        httpContext.Response.Cookies.Append(cookieName, anonId, new CookieOptions
        {
            Expires = DateTime.UtcNow.AddYears(1), // Long-lived cookie
            HttpOnly = true, // Prevent JavaScript access
            Secure = true,   // Send only over HTTPS
            SameSite = SameSiteMode.Lax
        });

        return anonId;
    }

    
    /// <inheritdoc/>>
    public string GetClientIpAddress(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress;
        
        if(ip == null || IPAddress.IsLoopback(ip))
            return "127.0.0.1";
        
        // If it's IPv6, map to IPv4
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            ip = ip.MapToIPv4();
        
        return ip.ToString();
    }

    
    
    #region Private Helper Methods

    /// <summary>
    /// Creates an authentication context for anonymous (unauthenticated) users.
    /// Sets the owner type to anonymous and assigns a persistent anonymous session ID.
    /// </summary>
    /// <param name="context">The base authentication context to populate</param>
    /// <param name="httpContext">The current HTTP context</param>
    /// <returns>An <see cref="IAuthenticationContext"/> configured for anonymous access</returns>
    private IAuthenticationContext CreateAnonymousContext(AuthenticationContext context, HttpContext httpContext)
    {
        context.IsAuthenticated = false;
        context.OwnerType = enShortUrlOwnerType.Anonymous;
        context.AnonymousSessionId = GetAnonymousId(httpContext);
        return context;
    }

    /// <summary>
    /// Populates the authentication context with details for authenticated users.
    /// Determines owner type based on available claims: User if UserId claim exists,
    /// Organization if OrganizationId claim exists (with optional MemberId).
    /// </summary>
    /// <param name="context">The authentication context to populate</param>
    /// <param name="httpContext">The current HTTP context containing user claims</param>
    private void PopulateAuthenticatedContext(AuthenticationContext context, HttpContext httpContext)
    {
        context.IsAuthenticated = true;

        if (TryGetGuidClaim(httpContext, ClaimUserId, out var userId))
        {
            context.UserId = userId;
            context.OwnerType = enShortUrlOwnerType.User;
            return;
        }

        if (TryGetGuidClaim(httpContext, ClaimOrganizationId, out var organizationId))
        {
            context.OrganizationId = organizationId;
            context.OwnerType = enShortUrlOwnerType.Organization;
            
            if (TryGetGuidClaim(httpContext, ClaimMemberId, out var memberId))
                context.MemberId = memberId;
        }
    }

    /// <summary>
    /// Attempts to extract and parse a GUID value from the specified claim type.
    /// </summary>
    /// <param name="httpContext">The current HTTP context containing user claims</param>
    /// <param name="claimType">The type of claim to search for</param>
    /// <param name="value">
    /// When this method returns, contains the parsed GUID if successful,
    /// or <see cref="Guid.Empty"/> if the claim doesn't exist or cannot be parsed.
    /// </param>
    /// <returns>
    /// True if the claim exists and contains a valid GUID; otherwise, false.
    /// </returns>
    private bool TryGetGuidClaim(HttpContext httpContext, string claimType, out Guid value)
    {
        value = Guid.Empty;
        var claimValue = httpContext.User.FindFirst(claimType)?.Value;
        return claimValue != null && Guid.TryParse(claimValue, out value);
    }

    #endregion
    
    

}