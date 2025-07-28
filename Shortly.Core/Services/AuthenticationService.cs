using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.DTOs.ValidationDTOs;
using Shortly.Core.Extensions;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;

namespace Shortly.Core.Services;

public class AuthenticationService(IUserRepository userRepository,IRefreshTokenRepository refreshTokenRepository,
    IConfiguration configuration, ILogger<AuthenticationService> logger): IAuthenticationService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly ILogger<AuthenticationService> _logger = logger;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly IConfigurationSection _jwtSection = configuration.GetSection("Jwt");

    
    public async Task<AuthenticationResponse?> Login(LoginRequest loginRequest)
    {
        var user = await _userRepository.GetActiveUserByEmail(loginRequest.Email);
        if (user == null)
            throw new AuthenticationException("Invalid email or password.");

        //Verify the password matches the hashed password
        if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            throw new AuthenticationException("Invalid email or password.");

        var tokensResponse = await GenerateTokensAsync(user);
        
        return new AuthenticationResponse(user.Id, user.Name, user.Email, tokensResponse, true);
    }

    public async Task<AuthenticationResponse?> Register(RegisterRequest registerRequest)
    {
        bool userExists =
            await _userRepository.IsEmailOrUsernameTaken(registerRequest.Email, registerRequest.Username);
        
        if (userExists)
            throw new Exception("User with the same username or email already exists.");
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = registerRequest.Name,
            Email = registerRequest.Email,
            Username = registerRequest.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password,10), // Hash the PasswordHash
        };
        
        user = await _userRepository.AddUser(user);
        if(user == null) throw new Exception("Error creating user");
        
        var tokensResponse = await GenerateTokensAsync(user);
        return new AuthenticationResponse(user.Id, user.Name, user.Email, tokensResponse, true);
    }
    
    public async Task<TokenResponse> GenerateTokensAsync(User user)
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

    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var storedRefreshToken = await _refreshTokenRepository
            .GetRefreshTokenAsync(SHA256Extensions.ComputeHash(refreshToken));
            
        // Validate refresh token
        if (storedRefreshToken == null)
        {
            _logger.LogWarning("Invalid refresh token attempted");
            throw new SecurityTokenException("Invalid refresh token");
        }

        // Remove the Invalid refresh token
        if (!storedRefreshToken.IsActive)
        {
            _logger.LogWarning("Inactive refresh token attempted for user {UserId}", storedRefreshToken.UserId);
            await _refreshTokenRepository.RemoveRefreshTokenAsync(storedRefreshToken);
            throw new SecurityTokenException("Invalid refresh token");
        }

        // Generate new refresh token string
        var newRefreshToken = GenerateRefreshTokenString();
            
        // Update Refresh Token entity
        storedRefreshToken.Token = SHA256Extensions.ComputeHash(newRefreshToken);
        await _refreshTokenRepository.SaveChangesAsync();

        // Generate new tokens
        var newAccessToken = GenerateAccessToken(storedRefreshToken.User);

        _logger.LogInformation("Tokens refreshed successfully for user {UserId}", storedRefreshToken.User.Id);
        return new TokenResponse
        (
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_jwtSection["AccessTokenExpirationMinutes"])),
            RefreshTokenExpiry: storedRefreshToken.ExpiresAt
        );
    }
    
   
    public TokenValidationResultDto ValidateToken(string token, bool validateLifetime = true)
    {
        try
        {
            var principal = GetPrincipalFromExpiredToken(token, validateLifetime);

            return new TokenValidationResultDto
            (
                IsValid: true,
                Principal: principal
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return new TokenValidationResultDto
            (
                IsValid: false,
                ErrorMessage: ex.Message
            );
        }
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
       
        var storedRefreshToken = await _refreshTokenRepository
            .GetRefreshTokenAsync(SHA256Extensions.ComputeHash(refreshToken));
        
        if (storedRefreshToken != null && storedRefreshToken.IsActive)
        {
            storedRefreshToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateRefreshTokenAsync(storedRefreshToken);
            _logger.LogInformation("Refresh token revoked: {Token}", refreshToken);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        var activeTokens = await _refreshTokenRepository.GetAllActiveRefreshTokensByUserIdAsync(userId);
        if (activeTokens == null || activeTokens.Count == 0) return;
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            await _refreshTokenRepository.UpdateRefreshTokenAsync(token);
        }
        _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
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
        var key = Encoding.UTF8.GetBytes(_jwtSection["Key"] ?? throw new InvalidOperationException());
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
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
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
            Token = SHA256Extensions.ComputeHash(refreshTokenString), // Hash to store in the database
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? throw new InvalidOperationException())),
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            IsRevoked = false
        };

        var savedToken = await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken);
        if (savedToken == null)
        {
            throw new InvalidOperationException("Failed to save refresh token");
        }
        savedToken.Token = refreshTokenString; // Re-assign the token with unhashed token

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSection["Key"] ?? throw new InvalidOperationException())),
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