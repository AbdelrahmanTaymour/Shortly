/*using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;

namespace Shortly.Core.Authentication;

public class JwtHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;
    private readonly IConfigurationSection _jwtSection;

    public JwtHandler(IConfiguration configuration, IRefreshTokenRepository refreshTokenRepository)
    {
        _configuration = configuration;
        _jwtSection = _configuration.GetSection("Jwt");
        _refreshTokenRepository = refreshTokenRepository;
    }

    // Create Access Token
    public string CreateAccessToken(User user)
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
        return
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        ];
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

    // Create Refresh Token
    /*private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationTokenParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSection["Issuer"],
            ValidAudience = _jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSection["Key"])),
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken securityToken;
        var principal = tokenHandler.ValidateToken(token, tokenValidationTokenParameters, out securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken is null || !jwtSecurityToken.Header.Alg
                .Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }
        return principal;
    }#1#
    
    public async Task<string?> GenerateRefreshToken(Guid userId)
    {
        var randomNumber = new byte[32];
        var refreshToken = "";
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            refreshToken = Convert.ToBase64String(randomNumber);
        }

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"])),
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            IsRevoked = false
        };

        var entity = await _refreshTokenRepository.AddRefreshToken(refreshTokenEntity);
        if (entity is null) return null;
        
        return entity.Token;
    }

        /*public async Task<TokenResponse> RefreshTokenAsync(string token)
        {
            var p = GetPrincipalFromExpiredToken(token);
            p.Identity.
            
            
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            // Get user details
            var user = await _context.Users.FindAsync(refreshToken.UserId);
            if (user == null)
            {
                throw new SecurityTokenException("User not found");
            }

            // Revoke the old refresh token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedDate = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;

            // Generate new tokens
            var newAccessToken = GenerateAccessToken(user.Id, user.Email);
            var newRefreshToken = GenerateRefreshToken(user.Id, ipAddress);
            
            // Link the old and new tokens
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            
            // Save new refresh token
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                RefreshTokenExpiry = newRefreshToken.ExpiryDate
            };
        }#1#
}*/