using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Extensions;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.Authentication;

/// <summary>
/// Provides methods for generating, validating, and revoking JWT access and refresh tokens.
/// </summary>
public class TokenService(
    IConfiguration configuration,
    ILogger<TokenService> logger,
    IRefreshTokenRepository refreshTokenRepository) : ITokenService
{
    private readonly IConfigurationSection _jwtSection = configuration.GetSection("Jwt");

    /// <inheritdoc />
    public async Task<TokenResponse> GenerateTokensAsync(User user)
    {
        // Revoke any existing active refresh tokens for this user
        await RevokeAllUserTokensAsync(user.Id);

        // Generate an access token
        var accessToken = GenerateAccessToken(user);
        var accessTokenExpiry =
            DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSection["AccessTokenExpirationMinutes"]));

        // Generate refresh token
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        logger.LogInformation("Tokens generated successfully for user {UserId}", user.Id);
        return new TokenResponse
        (
            accessToken,
            refreshToken.TokenHash,
            accessTokenExpiry,
            refreshToken.ExpiresAt
        );
    }

    /// <inheritdoc />
    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var storedRefreshToken = await refreshTokenRepository
            .GetRefreshTokenAsync(Sha256Extensions.ComputeHash(refreshToken));

        // Validate refresh token
        if (storedRefreshToken == null)
        {
            logger.LogWarning("Invalid refresh token attempted");
            throw new UnauthorizedException("Refresh token is invalid or has expired.");
        }

        // Remove the Invalid refresh token
        if (!storedRefreshToken.IsActive)
        {
            logger.LogWarning("Inactive refresh token attempted for user {UserId}", storedRefreshToken.UserId);
            await refreshTokenRepository.RemoveRefreshTokenAsync(storedRefreshToken);
            throw new UnauthorizedException("Refresh token is no longer active.");
        }

        // Generate new refresh token string
        var newRefreshToken = GenerateRefreshTokenString();

        // Update Refresh Token entity
        storedRefreshToken.TokenHash = Sha256Extensions.ComputeHash(newRefreshToken);
        await refreshTokenRepository.SaveChangesAsync();

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(storedRefreshToken.User);

        logger.LogInformation("Tokens refreshed successfully for user {UserId}", storedRefreshToken.User.Id);
        return new TokenResponse
        (
            newAccessToken,
            newRefreshToken,
            DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSection["AccessTokenExpirationMinutes"])),
            storedRefreshToken.ExpiresAt
        );
    }

    /// <inheritdoc />
    public TokenValidationResultDto ValidateToken(string token, bool validateLifetime = true)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(token, validateLifetime);

            return new TokenValidationResultDto
            (
                true,
                principal
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Token validation failed");
            return new TokenValidationResultDto
            (
                false,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var result = await refreshTokenRepository.RevokeRefreshTokenAsync(Sha256Extensions.ComputeHash(refreshToken),
            cancellationToken);

        if (result) logger.LogInformation("Refresh token revoked: {Token}", refreshToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await refreshTokenRepository.RevokeAllRefreshTokensAsync(userId, cancellationToken);
    }

    public string GenerateRedirectToken(string shortCode, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSection["Key"] ?? throw new InvalidOperationException()));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("shortCode", shortCode),
            new Claim("type", "redirect")
        };

        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime), // very short lifetime
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? ValidateRedirectToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSection["Key"] ?? throw new InvalidOperationException())),
                ValidateIssuerSigningKey = true
            }, out _);

            return principal.Claims.FirstOrDefault(c => c.Type == "shortCode")?.Value;
        }
        catch
        {
            return null;
        }
    }

    #region Private Methods

    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    /// <param name="user">The user to generate the token for.</param>
    /// <returns>A signed JWT access token.</returns>
    private string GenerateAccessToken(User user)
    {
        var signingCredentials = GetSigningCredentials();
        var claims = GetClaims(user);
        var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    /// <summary>
    /// Retrieves signing credentials from the JWT configuration.
    /// </summary>
    /// <returns>Signing credentials using HMAC SHA256 algorithm.</returns>
    /// <exception cref="ConfigurationException">Thrown when the JWT signing key is missing in the configuration.</exception>
    private SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(_jwtSection["Key"]
                                         ?? throw new ConfigurationException(
                                             "JWT signing key is missing in configuration."));
        var secret = new SymmetricSecurityKey(key);
        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Builds a list of JWT claims from the user object.
    /// </summary>
    /// <param name="user">The user for whom to create claims.</param>
    /// <returns>A list of claims including user ID, name, email, and permissions.</returns>
    private List<Claim> GetClaims(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("Permissions", ((long)user.Permissions).ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        return claims;
    }

    /// <summary>
    /// Constructs a <see cref="JwtSecurityToken"/> with the provided signing credentials and claims.
    /// </summary>
    /// <param name="signingCredentials">The credentials used to sign the token.</param>
    /// <param name="claims">The claims to include in the token.</param>
    /// <returns>A configured JWT token object.</returns>
    private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
    {
        return new JwtSecurityToken(
            _jwtSection["Issuer"],
            _jwtSection["Audience"],
            expires: DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_jwtSection["AccessTokenExpirationMinutes"])), // Token expiration
            claims: claims,
            signingCredentials: signingCredentials
        );
    }

    /// <summary>
    /// Generates a secure, random refresh token string.
    /// </summary>
    /// <returns>A base64-encoded refresh token string.</returns>
    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Creates and stores a hashed refresh token for the given user ID.
    /// </summary>
    /// <param name="userId">The ID of the user to associate with the token.</param>
    /// <returns>The saved refresh token entity, with unhashed token in the TokenHash property.</returns>
    /// <exception cref="DatabaseException">Thrown if storing the refresh token fails.</exception>
    /// <exception cref="ConfigurationException">Thrown if the refresh token expiration config is missing.</exception>
    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshTokenString = GenerateRefreshTokenString();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = Sha256Extensions.ComputeHash(refreshTokenString), // Hash to store in the database
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(configuration["Jwt:RefreshTokenExpirationDays"]
                                                             ?? throw new ConfigurationException(
                                                                 "JWT:RefreshTokenExpirationDays config is missing."))),
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            IsRevoked = false
        };

        var savedToken = await refreshTokenRepository.AddRefreshTokenAsync(refreshToken);
        if (savedToken == null)
            throw new DatabaseException("Failed to persist refresh token.");

        savedToken.TokenHash = refreshTokenString; // Re-assign the token with unhashed token
        return savedToken;
    }

    /// <summary>
    /// Extracts the claims principal from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token to validate and parse.</param>
    /// <param name="validateLifetime">Whether to validate token expiration.</param>
    /// <returns>The <see cref="ClaimsPrincipal"/> extracted from the token.</returns>
    /// <exception cref="ConfigurationException">Thrown if issuer, audience, or signing key is missing.</exception>
    /// <exception cref="ValidationException">Thrown when the token algorithm is invalid.</exception>
    /// <exception cref="SecurityTokenException">Thrown when token validation fails.</exception>
    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token, bool validateLifetime = true)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSection["Issuer"],
            ValidAudience = _jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSection["Key"]
                                                                               ?? throw new ConfigurationException(
                                                                                   "JWT signing key is missing in configuration."))),
            ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;

        if (jwtSecurityToken is null || !jwtSecurityToken.Header.Alg
                .Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new ValidationException("JWT token algorithm is invalid.");
        return principal;
    }

    #endregion
}