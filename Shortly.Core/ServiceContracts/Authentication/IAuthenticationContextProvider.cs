using Microsoft.AspNetCore.Http;

namespace Shortly.Core.ServiceContracts.Authentication;


/// <summary>
/// Defines the contract for providing authentication context information from HTTP requests.
/// Implementations should handle both authenticated users and anonymous sessions,
/// providing consistent identification and context determination across the application.
/// </summary>
public interface IAuthenticationContextProvider
{
    /// <summary>
    /// Gets the authentication context for the current HTTP request,
    /// determining the owner type and populating relevant authentication information.
    /// </summary>
    /// <param name="httpContext">The current HTTP context</param>
    /// <returns>
    /// An <see cref="IAuthenticationContext"/> containing authentication information.
    /// Returns anonymous context if user is not authenticated, otherwise returns
    /// context with user or organization details based on available claims.
    /// </returns>
    IAuthenticationContext GetCurrentContextAsync(HttpContext httpContext);
    
    
    /// <summary>
    /// Determines whether the current user is authenticated based on the HTTP context.
    /// </summary>
    /// <param name="httpContext">The current HTTP context</param>
    /// <returns>True if the user is authenticated, false otherwise</returns>
    bool IsAuthenticated(HttpContext httpContext);
    
    
    /// <summary>
    ///     Gets or creates a unique anonymous identifier for the current session.
    ///     If an anonymous ID cookie exists, returns its value. Otherwise, generates
    ///     a new GUID and sets it as a long-lived, secure cookie.
    /// </summary>
    /// <param name="httpContext">The current HTTP context from which to retrieve or set the anonymous identifier.</param>
    /// <returns>
    ///     The anonymous identifier as a <see cref="string"/>. 
    ///     If an existing cookie is found, its value is returned; otherwise, a new identifier is generated and returned.
    /// </returns>
    /// <remarks>
    ///     This method is typically used to persistently track anonymous users across multiple requests 
    ///     without requiring authentication. The identifier is stored in a secure, HTTP-only cookie 
    ///     named <c>AnonId</c> that expires after one year.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var anonId = GetAnonymousId(HttpContext);
    /// Console.WriteLine($"Anonymous User ID: {anonId}");
    /// </code>
    /// </example>
    string GetAnonymousId(HttpContext httpContext);

    
    /// <summary>
    /// Extracts the client's IP address from the HTTP context with fallback handling.
    /// Handles IPv6 to IPv4 mapping and provides localhost fallback for local connections.
    /// </summary>
    /// <param name="httpContext">The current HTTP context</param>
    /// <returns>
    /// A string representation of the client's IP address.
    /// Returns "127.0.0.1" for localhost/loopback connections.
    /// IPv6 addresses are mapped to IPv4 when possible.
    /// </returns>
    string GetClientIpAddress(HttpContext httpContext);
}