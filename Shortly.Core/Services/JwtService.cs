using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;

namespace Shortly.Core.Services;

public class JwtService(IRefreshTokenRepository refreshTokenRepository, 
    IConfiguration configuration, ILogger<JwtService> logger): IJwtService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly ILogger<JwtService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IConfigurationSection _jwtSection = configuration.GetSection("Jwt");
    

    public async Task<TokenResponse> GenerateTokensAsync(User user)
    {
        try
        {
           // Revoke any existing active refresh tokens for this user
            await RevokeAllUserTokensAsync(user.Id);
            
            // Generate an access token
            var accessToken = GenerateAccessToken(user);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSection["AccessTokenExpirationMinutes"]));

            // Generate refresh token
            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            

            _logger.LogInformation("Tokens generated successfully for user {UserId}", user.Id);
            return new TokenResponse
            (
                AccessToken: accessToken,
                RefreshToken: refreshToken.Token,
                AccessTokenExpiry: accessTokenExpiry,
                RefreshTokenExpiry: refreshToken.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tokens for user {UserId}", user.Id);
            throw new InvalidOperationException("Failed to generate authentication tokens", ex);
        }
    }

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, bool extendExpiry)
    {
        try
        {
            // Validate refresh token
            var storedRefreshToken = await _refreshTokenRepository.GetRefreshTokenAsync(refreshToken);
            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                _logger.LogWarning("Invalid refresh token attempted: {Token}", refreshToken);
                throw new SecurityTokenException("Invalid refresh token");
            }
            
            // Get user from token
            var user = await GetUserFromRefreshTokenAsync(storedRefreshToken);
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token: {UserId}", storedRefreshToken.UserId);
                throw new SecurityTokenException("User not found");
            }

            // Update refresh token
            storedRefreshToken.Token = GenerateRefreshTokenString();
            if(extendExpiry)
            {
                storedRefreshToken.ExpiresAt =
                    DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]));
            }
            await _refreshTokenRepository.SaveChangesAsync();

            // Generate new tokens
            var newAccessToken = GenerateAccessToken(user);

            _logger.LogInformation("Tokens refreshed successfully for user {UserId}", user.Id);

            return new TokenResponse
            (
                AccessToken: newAccessToken,
                RefreshToken: storedRefreshToken.Token,
                AccessTokenExpiry: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSection["AccessTokenExpirationMinutes"])),
                RefreshTokenExpiry: storedRefreshToken.ExpiresAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            throw;
        }
    }
    
    public TokenValidationResulDto ValidateToken(string token, bool validateLifetime = true)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(token, validateLifetime);

            return new TokenValidationResulDto
            (
                IsValid: true,
                Principal: principal
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return new TokenValidationResulDto
            (
                IsValid: false,
                ErrorMessage: ex.Message
            );
        }
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        try
        {
            var storedRefreshToken = await _refreshTokenRepository.GetRefreshTokenAsync(refreshToken);
            if (storedRefreshToken != null && storedRefreshToken.IsActive)
            {
                storedRefreshToken.IsRevoked = true;
                await _refreshTokenRepository.UpdateRefreshTokenAsync(storedRefreshToken);
                _logger.LogInformation("Refresh token revoked: {Token}", refreshToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            throw;
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            var activeTokens = await _refreshTokenRepository.GetAllActiveRefreshTokensByUserIdAsync(userId);
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                await _refreshTokenRepository.UpdateRefreshTokenAsync(token);
            }
            _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all user tokens");
            throw;
        }
    }

    
    
    
    #region Private Methods

    private string GenerateAccessToken(User user)
    {
        var signingCredentials = GetSigningCredentials();
        var claims = GetClaims(user);
        var tokenOptions = GenerateTokenOptions(signingCredentials, claims);
        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    private SigningCredentials GetSigningCredentials()
    {
        var key = Encoding.UTF8.GetBytes(_jwtSection["Key"]);
        var secret = new SymmetricSecurityKey(key);
        return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
    }

    private List<Claim> GetClaims(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // TODO: Add role claims
        // if (user.Roles != null)
        // {
        //     claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        // }

        return claims;
    }

    private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
    {
        return new JwtSecurityToken(
            issuer: _jwtSection["Issuer"],
            audience: _jwtSection["Audience"],
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSection["AccessTokenExpirationMinutes"])), // Token expiration
            claims: claims,
            signingCredentials: signingCredentials
        );
    }

    private string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshTokenString = GenerateRefreshTokenString();
        
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"])),
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            IsRevoked = false
        };

        var savedToken = await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken);
        if (savedToken == null)
        {
            throw new InvalidOperationException("Failed to save refresh token");
        }

        return savedToken;
    }

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSection["Key"])),
            ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        
        if (jwtSecurityToken is null || !jwtSecurityToken.Header.Alg
                .Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm");
        }
        return principal;
    }
    
    private async Task<User?> GetUserFromRefreshTokenAsync(RefreshToken refreshToken)
    {
        return await _refreshTokenRepository.GetUserFromRefreshTokenAsync(refreshToken.UserId);
    }

    #endregion

}