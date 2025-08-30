using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UserActionTokensDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Extensions;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.Tokens;
using Shortly.Core.ServiceContracts.Tokens;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.Tokens;

public class UserActionTokenService(
    IUserActionTokenRepository userActionTokenRepository,
    IConfiguration configuration,
    ILogger<IUserActionTokenService> logger
) : IUserActionTokenService
{
    private readonly IConfigurationSection _tokensSection = configuration.GetSection("AppSettings:Tokens");
    
    public async Task<UserActionTokenDto> GenerateTokenAsync(Guid userId, enUserActionTokenType tokenType, TimeSpan? customExpiration = null, CancellationToken cancellationToken = default)
    {
        // Invalidate any existing active tokens of the same type for this user
        await InvalidateUserTokensAsync(userId, tokenType, cancellationToken);

        // Generate a token using GUID
        var plainToken = Guid.NewGuid().ToString();
        var lifetime = customExpiration ?? _GetExpirationForTokenType(tokenType);

        var token = new UserActionToken
        {
            UserId = userId,
            TokenType = tokenType,
            TokenHash = Sha256Extensions.ComputeHash(plainToken),
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            Used = false
        };

        var createdToken = await userActionTokenRepository.CreateAsync(token, cancellationToken);
        logger.LogInformation("Generated {TokenType} token for user {UserId}", tokenType, userId);
        
        // Return the token with the plain text token
        return createdToken.MapToUserActionTokenDto(plainToken);
    }
    
    public async Task<bool> ValidateTokenAsync(string tokenHash, enUserActionTokenType expectedTokenType, CancellationToken cancellationToken = default)
    {
        var storedToken = await userActionTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken == null)
        {
            logger.LogWarning("Token validation failed: token not found");
            throw new NotFoundException("Verification token not found.");
        }

        if (storedToken.TokenType != expectedTokenType)
        {
            logger.LogWarning("Token validation failed: incorrect token type. Expected {ExpectedType}, got {ActualType}", expectedTokenType, storedToken.TokenType);
            throw new ForbiddenException("Incorrect token type.");
        }

        if (storedToken.Used)
        {
            logger.LogWarning("Token validation failed: token already used for user {UserId}", storedToken.UserId);
            throw new ConflictException("Token already used.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            logger.LogWarning("Token validation failed: token expired for user {UserId}", storedToken.UserId);
            throw new ForbiddenException("This verification link has expired.");
        }

        return true;
    }

    public async Task<UserActionTokenDto> GetTokenDetailsAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = Sha256Extensions.ComputeHash(token);
        var storedToken = await userActionTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (storedToken == null)
            throw new NotFoundException("Verification token not found.");

        return storedToken.MapToUserActionTokenDto(token);
    }

    public async Task<bool> ConsumeTokenAsync(string token, enUserActionTokenType tokenType, CancellationToken cancellationToken = default)
    {
        var tokenHash = Sha256Extensions.ComputeHash(token);
        await ValidateTokenAsync(tokenHash, tokenType, cancellationToken);

        return await userActionTokenRepository.ConsumeTokenAsync(tokenHash, tokenType, cancellationToken);
    }

    public async Task<bool> InvalidateUserTokensAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default)
    {
        return await userActionTokenRepository.InvalidateUserTokensAsync(userId, tokenType, cancellationToken);
    }

    public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        return await userActionTokenRepository.DeleteExpiredTokensAsync(cancellationToken);
    }

    public async Task<bool> HasActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default)
    {
        return await userActionTokenRepository.HasActiveTokenAsync(userId, tokenType, cancellationToken);
    }

    private TimeSpan _GetExpirationForTokenType(enUserActionTokenType tokenType)
    {
        return tokenType switch
        {
            enUserActionTokenType.EmailVerification =>
                GetConfiguredTimeSpan("EmailVerificationExpiryHours", 24),
        
            enUserActionTokenType.PasswordReset =>
                GetConfiguredTimeSpan("PasswordResetExpiryHours", 1),
        
            _ => TimeSpan.FromDays(1)
        };
    }

    private TimeSpan GetConfiguredTimeSpan(string configKey, int defaultHours)
    {
        var configValue = _tokensSection[configKey];
        if (string.IsNullOrEmpty(configValue) || !double.TryParse(configValue, out var hours))
        {
            logger.LogWarning("Invalid or missing configuration for {ConfigKey}, using default {DefaultHours} hours", 
                configKey, defaultHours);
            return TimeSpan.FromHours(defaultHours);
        }
        return TimeSpan.FromHours(hours);
    }
}