using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Extensions;
using Shortly.Core.Models;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.ClickTracking;
using Shortly.Core.ServiceContracts.UrlManagement;

namespace Shortly.Core.Services.UrlManagement;

/// <summary>
///     Provides operations for retrieving and managing redirect-related information
///     for shortened URLs, including password verification, click tracking,
///     and validity checks.
/// </summary>
/// <remarks>
///     This service acts as a business layer over <see cref="IShortUrlRedirectRepository" />
///     to handle additional validation, error handling, and exception throwing for
///     redirect-related operations.
/// </remarks>
public class ShortUrlRedirectService(
    IShortUrlRedirectRepository redirectRepository,
    ClickTrackingQueueService clickTrackingQueueService,
    IAuthenticationContextProvider authenticationContext,
    ILogger<ShortUrlRedirectService> logger) : IShortUrlRedirectService
{
    /// <inheritdoc />
    public async Task<UrlRedirectResult> GetRedirectInfoByShortCodeAsync(string shortCode, HttpContext context,
        CancellationToken cancellationToken = default)
    {
        var shortUrlRedirectInfo = await redirectRepository.GetRedirectInfoByShortCodeAsync(shortCode, cancellationToken);
        if (shortUrlRedirectInfo is null)
            throw new NotFoundException("ShortUrl", shortCode);

        if (!shortUrlRedirectInfo.CanAccess())
            throw new ForbiddenException("This link is no longer active.");
        
        // Increment click count
        await IncrementClickCountAsync(shortCode, cancellationToken);

        // Extract tracking data
        var trackingData = ExtractTrackingDataAsync(context, cancellationToken);

        // Enqueue the click tracking job for background processing
        clickTrackingQueueService.EnqueueClick(shortUrlRedirectInfo.Id, trackingData);

        // Return the redirect result immediately, while tracking executes in background
        return new UrlRedirectResult
        (
            shortUrlRedirectInfo.OriginalUrl,
            shortUrlRedirectInfo.IsPasswordProtected
        );
    }


    public ClickTrackingData ExtractTrackingDataAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        // IP Address (use X-Forwarded-For if behind proxy)
        var ipAddress = authenticationContext.GetClientIpAddress(context);

        // Session ID (Anonymous Id)
        var sessionId = authenticationContext.GetAnonymousId(context);

        // User Agent
        var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";

        // Referrer
        var referrer = context.Request.Headers["Referer"].FirstOrDefault();

        // UTM Parameters
        context.Request.Query.TryGetValue("utm_source", out var utmSource);
        context.Request.Query.TryGetValue("utm_medium", out var utmMedium);
        context.Request.Query.TryGetValue("utm_campaign", out var utmCampaign);
        context.Request.Query.TryGetValue("utm_term", out var utmTerm);
        context.Request.Query.TryGetValue("utm_content", out var utmContent);

        var data = new ClickTrackingData(
            ipAddress,
            sessionId,
            userAgent,
            referrer,
            utmSource,
            utmMedium,
            utmCampaign,
            utmTerm,
            utmContent
        );

        logger.LogDebug("Extracted ClickTrackingData: {@Data}", data);

        return data;
    }

    /// <inheritdoc />
    public async Task<bool> IncrementClickCountAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            throw new ArgumentException("Short code cannot be null or empty", nameof(shortCode));


        return await redirectRepository.IncrementClickCountAsync(shortCode, cancellationToken);
    }
    

    /// <inheritdoc />
    public async Task<bool> IsClickLimitReachedAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        return await redirectRepository.IsClickLimitReachedAsync(shortUrlId, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<bool> IsPasswordProtectedAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        return await redirectRepository.IsPasswordProtectedAsync(shortUrlId, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<bool> VerifyPasswordAsync(long shortUrlId, string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        var passwordHash = Sha256Extensions.ComputeHash(password);
        return await redirectRepository.VerifyPasswordAsync(shortUrlId, passwordHash, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<string?> GetUrlIfPasswordCorrectAsync(string shortCode, string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            throw new ArgumentException("Short code must be provided.", nameof(shortCode));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password hash must be provided.", nameof(password));

        var passwordHash = Sha256Extensions.ComputeHash(password);
        return await redirectRepository.GetUrlIfPasswordCorrectAsync(shortCode, passwordHash, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<bool> IsValidAsync(long shortUrlId, DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await redirectRepository.IsValidAsync(shortUrlId, nowUtc, cancellationToken);
    }


    /// <inheritdoc />
    public async Task<bool> IsActiveAsync(long shortUrlId, CancellationToken cancellationToken = default)
    {
        return await redirectRepository.IsActiveAsync(shortUrlId, cancellationToken);
    }
}