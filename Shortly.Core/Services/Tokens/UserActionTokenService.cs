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

/// <inheritdoc />
/// <param name="userActionTokenRepository">Repository for token data access operations.</param>
/// <param name="configuration">Configuration provider for token settings.</param>
/// <param name="logger">Logger instance for tracking operations and errors.</param>
public class UserActionTokenService(
    IUserActionTokenRepository userActionTokenRepository,
    IConfiguration configuration,
    ILogger<IUserActionTokenService> logger
) : IUserActionTokenService
{
    private readonly IConfigurationSection _tokensSection = configuration.GetSection("AppSettings:Tokens");
    
    /// <inheritdoc />
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
    
    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<UserActionTokenDto> GetTokenDetailsAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = Sha256Extensions.ComputeHash(token);
        var storedToken = await userActionTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (storedToken == null)
            throw new NotFoundException("Verification token not found.");

        return storedToken.MapToUserActionTokenDto(token);
    }

    /// <inheritdoc />
    public async Task<bool> ConsumeTokenAsync(string token, enUserActionTokenType tokenType, CancellationToken cancellationToken = default)
    {
        var tokenHash = Sha256Extensions.ComputeHash(token);
        await ValidateTokenAsync(tokenHash, tokenType, cancellationToken);

        return await userActionTokenRepository.ConsumeTokenAsync(tokenHash, tokenType, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> InvalidateUserTokensAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default)
    {
        return await userActionTokenRepository.InvalidateUserTokensAsync(userId, tokenType, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        return await userActionTokenRepository.DeleteExpiredTokensAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveTokenAsync(Guid userId, enUserActionTokenType tokenType, CancellationToken cancellationToken = default)
    {
        return await userActionTokenRepository.HasActiveTokenAsync(userId, tokenType, cancellationToken);
    }

    /// <summary>
    /// Gets the default expiration time for a specific token type from configuration.
    /// </summary>
    /// <param name="tokenType">The token type to get expiration for.</param>
    /// <returns>
    /// The configured expiration TimeSpan for the token type, or a default value if not configured.
    /// </returns>
    /// <remarks>
    /// This method reads from the configuration section "AppSettings:Tokens" and looks for
    /// type-specific expiration settings. Falls back to 24 hours if no configuration is found.
    /// </remarks>
    private TimeSpan _GetExpirationForTokenType(enUserActionTokenType tokenType)
    {
        return tokenType switch
        {
            enUserActionTokenType.EmailVerification =>
                _GetConfiguredTimeSpan("EmailVerificationExpiryHours", 24),
        
            enUserActionTokenType.PasswordReset =>
                _GetConfiguredTimeSpan("PasswordResetExpiryHours", 1),
        
            _ => TimeSpan.FromDays(1)
        };
    }

    /// <summary>
    /// Retrieves a configured time span value or returns a default if configuration is invalid.
    /// </summary>
    /// <param name="configKey">The configuration key to look up.</param>
    /// <param name="defaultHours">The default number of hours to use if the configuration is missing or invalid.</param>
    /// <returns>
    /// A TimeSpan representing the configured or default expiration time.
    /// </returns>
    /// <remarks>
    /// This method handles configuration parsing and validation, logging warnings
    /// when invalid or missing configuration values are encountered.
    /// </remarks>
    private TimeSpan _GetConfiguredTimeSpan(string configKey, int defaultHours)
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