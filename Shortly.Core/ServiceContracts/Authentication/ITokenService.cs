using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Domain.Entities;

namespace Shortly.Core.ServiceContracts.Authentication;

/// <summary>
/// Defines methods for generating, validating, and revoking access and refresh tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a new access and refresh token pair for the specified user.
    /// </summary>
    /// <param name="user">The user to generate tokens for.</param>
    /// <returns>A <see cref="TokenResponse"/> containing token data.</returns>
    Task<TokenResponse> GenerateTokensAsync(User user);

    /// <summary>
    /// Refreshes the token pair using a valid refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to use for generating new tokens.</param>
    /// <returns>A new <see cref="TokenResponse"/> if successful; otherwise, null.</returns>
    /// <exception cref="UnauthorizedException">Thrown if the token is invalid, expired, or inactive.</exception>
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Validates a given JWT token and returns the result.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="validateLifetime">Whether to validate token expiration.</param>
    /// <returns>A <see cref="TokenValidationResultDto"/> indicating validation status.</returns>
    TokenValidationResultDto ValidateToken(string token, bool validateLifetime = true);

    /// <summary>
    /// Revokes a specific refresh token if it is active.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <param name="cancellationToken"></param>
    Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh tokens for a given user.
    /// </summary>
    /// <param name="userId">The ID of the user whose tokens should be revoked.</param>
    Task<bool> RevokeAllUserTokensAsync(Guid userId);
}