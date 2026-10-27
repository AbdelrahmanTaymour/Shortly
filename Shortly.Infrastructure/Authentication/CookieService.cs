using Microsoft.AspNetCore.Http;
using Shortly.Core.Abstractions.Authentication;

namespace Shortly.Infrastructure.Authentication;

internal sealed class CookieService(IHttpContextAccessor httpContextAccessor) : ICookieService
{
    private const string CookieName = "refreshToken";

    /// <summary>
    /// Retrieves the stored refresh token from the HTTP request cookies.
    /// Returns null if the cookie is not present or the HTTP context is unavailable.
    /// </summary>
    /// <returns>The refresh token if present; otherwise, null.</returns>
    public string? GetRefreshToken() =>
        httpContextAccessor.HttpContext?.Request.Cookies[CookieName];

    /// <summary>
    /// Stores the refresh token in a secure cookie with specified expiration settings.
    /// The cookie is configured to be HTTP-only, secure, and uses SameSite=None policy.
    /// </summary>
    /// <param name="refreshToken">The refresh token to be stored.</param>
    /// <param name="expiresAt">The expiration date and time of the refresh token.</param>
    public void SetRefreshToken(string refreshToken, DateTime expiresAt)
    {
        httpContextAccessor.HttpContext?.Response.Cookies.Append(CookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = expiresAt,
            IsEssential = true
        });
    }

    /// <summary>
    /// Deletes the refresh token by removing the associated cookie from the current HTTP response.
    /// The cookie is identified by its name and is configured to use the SameSite=None policy,
    /// HTTP-only, and secure attributes.
    /// </summary>
    public void DeleteRefreshToken()
    {
        httpContextAccessor.HttpContext?.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
    }
}