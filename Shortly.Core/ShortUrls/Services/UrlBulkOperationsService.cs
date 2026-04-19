using Microsoft.Extensions.Logging;
using Shortly.Core.Auth.Contracts;
using Shortly.Core.Common;
using Shortly.Core.Extensions;
using Shortly.Core.ShortUrls.Contracts;
using Shortly.Core.ShortUrls.DTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.ShortUrls.Services;

/// <summary>
/// Provides bulk operations for URL shortening services including creation, updates, and deletion.
/// Handles batch processing of multiple URLs with conflict resolution and validation.
/// </summary>
/// <param name="bulkOperationsCommand">Repository for performing bulk database operations.</param>
/// <param name="logger">Logger instance for operation tracking and debugging.</param>
public class UrlBulkOperationsService(IUrlBulkOperationsCommand bulkOperationsCommand, ILogger<UrlBulkOperationsService> logger) : IUrlBulkOperationsService
{
    
    /// <inheritdoc />
    public async Task<BulkCreateShortUrlResult> BulkCreateAsync(
    IReadOnlyCollection<CreateShortUrlRequest> shortUrlRequests, 
    IAuthenticationContext authContext, 
    CancellationToken cancellationToken = default)
    {
        if (shortUrlRequests == null || shortUrlRequests.Count == 0)
            throw new ArgumentException("Short URLs collection cannot be null or empty", nameof(shortUrlRequests));
        
        logger.LogInformation("Starting bulk creation for {RequestCount} short URLs.", shortUrlRequests.Count);
        
        var shortUrlsToInsert = new List<ShortUrl>();
        var conflictMessages = new List<string>();
        var processedRequests = 0;

        try
        {
            // Batch check all custom codes at once instead of individual checks
            var customCodes = shortUrlRequests
                .Where(r => !string.IsNullOrWhiteSpace(r.CustomShortCode))
                .Select(r => r.CustomShortCode!)
                .Distinct()
                .ToHashSet();

            HashSet<string?> existingCodes = customCodes.Count > 0 
                ? await bulkOperationsCommand.GetExistingCustomShortCodesAsync(customCodes, cancellationToken)
                : [];

            // Process requests with optimized conflict checking
            foreach (var request in shortUrlRequests)
            {
                processedRequests++;
                
                // Check for custom code conflicts using pre-fetched data
                if (!string.IsNullOrWhiteSpace(request.CustomShortCode) && 
                    existingCodes.Contains(request.CustomShortCode))
                {
                    conflictMessages.Add($"Custom short code '{request.CustomShortCode}' for URL '{request.OriginalUrl}' already exists.");
                    continue;
                }
                
                var shortUrl = BuildShortUrlEntity(request, authContext);
                shortUrlsToInsert.Add(shortUrl);
            }
            
            logger.LogInformation("Prepared {InsertCount} short URLs for database insertion.", shortUrlsToInsert.Count);
            
            // Insert URLs into database
            var createResult = shortUrlsToInsert.Count > 0 
                ? await bulkOperationsCommand.BulkCreateAsync(shortUrlsToInsert, cancellationToken)
                : new BulkOperationResult(0, 0, 0);
            
            // Generate and update short codes for URLs that need them
            var shortCodeUpdateResult = new BulkOperationResult(0, 0, 0);
            if (createResult.SuccessCount > 0)
            {
                var urlsNeedingCodes = AssignShortCodes(shortUrlsToInsert);
                if (urlsNeedingCodes.Count > 0)
                {
                    logger.LogInformation("Updating short codes for {Count} URLs.", urlsNeedingCodes.Count);
                    var idCodesMap = urlsNeedingCodes.ToDictionary(x => x.Id, x => x.ShortCode);
                    shortCodeUpdateResult = await bulkOperationsCommand.BulkUpdateShortCodeAsync(idCodesMap, cancellationToken);
                }
            }
            
            var totalFailures = createResult.FailureCount + shortCodeUpdateResult.FailureCount + conflictMessages.Count;
            
            logger.LogInformation("Bulk creation completed. Total: {Total}, Success: {Success}, Failures: {Failures}, Conflicts: {Conflicts}", 
                processedRequests, createResult.SuccessCount, totalFailures, conflictMessages.Count);
            
            return new BulkCreateShortUrlResult(
                TotalRequests: shortUrlRequests.Count,
                ProcessedCount: processedRequests,
                SuccessCount: createResult.SuccessCount,
                FailureCount: totalFailures,
                ConflictCount: conflictMessages.Count,
                ConflictMessages: conflictMessages
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during bulk creation of {RequestCount} short URLs", shortUrlRequests.Count);
            throw new InvalidOperationException($"Bulk creation failed after processing {processedRequests} requests", ex);
        }
    }
    
    
    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkUpdateExpirationAsync(IReadOnlyCollection<long> ids, DateTime? newExpirationDate,
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be null or empty", nameof(ids));

        return await bulkOperationsCommand.BulkUpdateExpirationAsync(ids, newExpirationDate, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeleteAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be null or empty", nameof(ids));
        
        return await bulkOperationsCommand.BulkDeleteAsync(ids, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<BulkOperationResult> DeleteExpiredAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await bulkOperationsCommand.DeleteExpiredAsync(nowUtc, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkActivateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be null or empty", nameof(ids));
        
        return await bulkOperationsCommand.BulkActivateAsync(ids, cancellationToken);
    }

    
    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeactivateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            throw new ArgumentException("IDs collection cannot be null or empty", nameof(ids));
        
        return await bulkOperationsCommand.BulkDeactivateAsync(ids, cancellationToken);
    }


    #region Private Methods

    
    /// <summary>
    /// Builds a ShortUrl entity from a creation request and authentication context.
    /// </summary>
    /// <param name="request">The request containing URL details and configuration.</param>
    /// <param name="authContext">The authentication context with user and organization information.</param>
    /// <returns>A configured <see cref="ShortUrl"/> entity ready for database insertion.</returns>
    /// <remarks>
    /// Automatically hashes the password if the URL is password-protected.
    /// The ShortCode will be set to the custom code if provided, otherwise null for later generation.
    /// </remarks>
    private ShortUrl BuildShortUrlEntity(CreateShortUrlRequest request,  IAuthenticationContext authContext)
    {
        var shortUrl = new ShortUrl
        {
            OriginalUrl = request.OriginalUrl,
            ShortCode = request.CustomShortCode,
            OwnerType = authContext.OwnerType,
            UserId = authContext.UserId,
            OrganizationId = authContext.OrganizationId,
            CreatedByMemberId = authContext.MemberId,
            AnonymousSessionId = authContext.AnonymousSessionId,
            IpAddress = authContext.IpAddress,
            ClickLimit = request.ClickLimit,
            TrackingEnabled = request.TrackingEnabled,
            IsPasswordProtected = request.IsPasswordProtected,
            IsPrivate = request.IsPrivate,
            ExpiresAt = request.ExpiresAt,
            Title = request.Title,
            Description = request.Description
        };

        if (request.IsPasswordProtected && !string.IsNullOrEmpty(request.Password))
            shortUrl.PasswordHash = Sha256Extensions.ComputeHash(request.Password);
        
        return shortUrl;
    }

    
    /// <summary>
    /// Generates short codes for URLs that don't have custom codes assigned.
    /// </summary>
    /// <param name="shortUrls">List of ShortUrl entities to process.</param>
    /// <returns>A list containing only the URLs that had codes generated (subset of input).</returns>
    /// <remarks>
    /// Uses the entity's ID to generate a unique short code via base62 encoding.
    /// </remarks>
    private List<ShortUrl> AssignShortCodes(List<ShortUrl> shortUrls)
    {
        var urlsNeedingCodes = shortUrls
            .Where(x => string.IsNullOrWhiteSpace(x.ShortCode))
            .ToList();
        
        foreach (var shortUrl in urlsNeedingCodes)
        {
            shortUrl.ShortCode = UrlCodeExtensions.GenerateCodeAsync(shortUrl.Id);
        }

        return urlsNeedingCodes;
    }

    #endregion
}