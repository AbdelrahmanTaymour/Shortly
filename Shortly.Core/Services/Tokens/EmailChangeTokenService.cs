using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.TokenDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.Tokens;
using Shortly.Core.ServiceContracts.Tokens;
using Shortly.Domain.Entities;

namespace Shortly.Core.Services.Tokens;

/// <inheritdoc />
public class EmailChangeTokenService(
    IEmailChangeTokenRepository emailChangeTokenRepository,
    IConfiguration configuration,
    ILogger<EmailChangeTokenService> logger
    ) : IEmailChangeTokenService
{
    private readonly IConfigurationSection _tokensSection = configuration.GetSection("AppSettings:Tokens");
    
    /// <inheritdoc />
    public async Task<EmailChangeTokenDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await emailChangeTokenRepository.GetByIdAsync(id, cancellationToken);
        if (token == null)
            throw new NotFoundException("EmailChangeToken", id);
        
        return token.MapToEmailChangeTokenDto();
    }

    /// <inheritdoc />
    public async Task<EmailChangeTokenDto> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var emailChangeToken = await emailChangeTokenRepository.GetByTokenAsync(token, cancellationToken);
        if (emailChangeToken == null)
            throw new NotFoundException("EmailChangeToken", token);
        
        return emailChangeToken.MapToEmailChangeTokenDto();
    }

    /// <inheritdoc />
    public async Task<EmailChangeTokenDto> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeEmailChangeToken = await emailChangeTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (activeEmailChangeToken == null)
            throw new NotFoundException("EmailChangeToken", userId);
        
        return activeEmailChangeToken.MapToEmailChangeTokenDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EmailChangeTokenDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await emailChangeTokenRepository.GetByUserIdAsync(userId, cancellationToken);
        return tokens.MapToEmailChangeTokenDtos();
    }

    /// <inheritdoc />
    public async Task<EmailChangeTokenDto> CreateTokenAsync(Guid userId, string oldEmail, string newEmail, CancellationToken cancellationToken = default)
    {
        // Check if the user already has an active token
        var existingToken = await emailChangeTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        if (existingToken != null)
            throw new InvalidOperationException("User already has an active email change token");

        var tokenLifeTime = _GetConfiguredTimeSpan(configKey: "EmailChangeTokenExpiryHours",  defaultHours: 1);
        var emailChangeToken = new EmailChangeToken
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString(),
            UserId = userId,
            OldEmail = oldEmail,
            NewEmail = newEmail,
            ExpiresAt = DateTime.UtcNow.Add(tokenLifeTime),
            IsUsed = false
        };

        var createdToken = await emailChangeTokenRepository.CreateAsync(emailChangeToken, cancellationToken);
        logger.LogInformation("Created email change token for user: {UserId}", userId);
        
        return createdToken.MapToEmailChangeTokenDto();
    }

    /// <inheritdoc />
    public async Task<EmailChangeTokenDto> UseTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var emailChangeToken = await emailChangeTokenRepository.GetByTokenAsync(token, cancellationToken);
        if (emailChangeToken == null)
            throw new NotFoundException("EmailChangeToken", token);

        if (emailChangeToken.IsUsed)
            throw new InvalidOperationException("Token has already been used");

        if (emailChangeToken.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Token has expired");

        // Mark token as used
        await emailChangeTokenRepository.UseTokenAsync(emailChangeToken.Id, cancellationToken);

        return emailChangeToken.MapToEmailChangeTokenDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTokenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await emailChangeTokenRepository.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        return await emailChangeTokenRepository.DeleteExpiredTokensAsync(cancellationToken);
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