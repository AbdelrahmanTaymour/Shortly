using Shortly.Core.DTOs.AuthDTOs;

namespace Shortly.Core.ServiceContracts.Authentication;

/// <summary>
/// Service for handling OAuth authentication operations
/// </summary>
public interface IOAuthService
{
    /// <summary>
    /// Authenticates or registers a user via Google OAuth
    /// </summary>
    /// <param name="googleEmail">Email from Google OAuth</param>
    /// <param name="googleId">Google user ID</param>
    /// <param name="googleName">User's name from Google</param>
    /// <param name="googlePicture">User's profile picture URL from Google</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthenticationResponse?> AuthenticateWithGoogleAsync(
        string googleEmail,
        string googleId,
        string? googleName,
        string? googlePicture,
        CancellationToken cancellationToken = default);
}