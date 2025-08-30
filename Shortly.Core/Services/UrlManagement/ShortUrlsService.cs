using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Core.Exceptions.ClientErrors;
using Shortly.Core.Exceptions.ServerErrors;
using Shortly.Core.Extensions;
using Shortly.Core.Mappers;
using Shortly.Core.RepositoryContract.UrlManagement;
using Shortly.Core.ServiceContracts.Authentication;
using Shortly.Core.ServiceContracts.OrganizationManagement;
using Shortly.Core.ServiceContracts.UrlManagement;
using Shortly.Core.ServiceContracts.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services.UrlManagement;

/// <summary>
///     Service for managing short URLs, providing CRUD operations and business logic
///     for creating, retrieving, updating, and deleting short URLs with support for
///     different owner types (anonymous, user, organization).
/// </summary>
/// <param name="shortUrlRepository">Repository for short URL data access operations</param>
/// <param name="authenticationContext">Provider for authentication context information</param>
/// <param name="logger">Logger for service operations</param>
public class ShortUrlsService(
    IShortUrlRepository shortUrlRepository,
    IAuthenticationContextProvider authenticationContext,
    IOrganizationUsageService organizationUsageService,
    IUserUsageService userUsageService,
    ILogger<ShortUrlsService> logger)
    : IShortUrlsService
{
    /// <inheritdoc />
    public async Task<ShortUrlDto> GetByIdAsync(long id, bool includeTracking = false,
        CancellationToken cancellationToken = default)
    {
        var url = await shortUrlRepository.GetByIdAsync(id, includeTracking, cancellationToken);
        if (url == null)
            throw new NotFoundException("Url", id);

        return url.MapToShortUrlDto();
    }


    /// <inheritdoc />
    public async Task<ShortUrlDto> GetByShortCodeAsync(string shortCode, bool includeTracking = false,
        CancellationToken cancellationToken = default)
    {
        var url = await shortUrlRepository.GetByShortCodeAsync(shortCode, includeTracking, cancellationToken);
        if (url == null)
            throw new NotFoundException("Url", shortCode);

        return url.MapToShortUrlDto();
    }


    /// <inheritdoc />
    public async Task<CreateShortUrlResponse> AddAsync(CreateShortUrlRequest request, HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var authContext = authenticationContext.GetCurrentContextAsync(httpContext);
        return authContext.OwnerType switch
        {
            enShortUrlOwnerType.Anonymous =>
                await HandleAnonymousCreationAsync(request, authContext, cancellationToken),
            
            enShortUrlOwnerType.User => 
                await HandleUserCreationAsync(request, authContext, cancellationToken),
            
            enShortUrlOwnerType.Organization => 
                await HandleOrganizationMemberCreationAsync(request, authContext, cancellationToken),
            
            _ => throw new InvalidOperationException("Unsupported owner type.")
        };
    }


    /// <inheritdoc />
    public async Task<ShortUrlDto> UpdateByIdAsync(long id, UpdateShortUrlRequest shortUrl,
        CancellationToken cancellationToken = default)
    {
        var existingUrl = await shortUrlRepository.GetByIdAsync(id, true, cancellationToken);
        if (existingUrl == null)
            throw new NotFoundException("Url", id);

        // Update entity
        existingUrl.OriginalUrl = shortUrl.OriginalUrl ?? existingUrl.OriginalUrl;
        existingUrl.IsActive = shortUrl.IsActive ?? existingUrl.IsActive;
        existingUrl.TrackingEnabled = shortUrl.TrackingEnabled ?? existingUrl.TrackingEnabled;
        existingUrl.ClickLimit = shortUrl.ClickLimit ?? existingUrl.ClickLimit;
        existingUrl.IsPasswordProtected = shortUrl.IsPasswordProtected ?? existingUrl.IsPasswordProtected;
        existingUrl.PasswordHash = shortUrl.Password != null
            ? Sha256Extensions.ComputeHash(shortUrl.Password)
            : existingUrl.PasswordHash;
        existingUrl.IsPrivate = shortUrl.IsPrivate ?? existingUrl.IsPrivate;
        existingUrl.ExpiresAt = shortUrl.ExpiresAt ?? existingUrl.ExpiresAt;
        existingUrl.Title = shortUrl.Title ?? existingUrl.Title;
        existingUrl.Description = shortUrl.Description ?? existingUrl.Description;
        existingUrl.UpdatedAt = DateTime.UtcNow;

        var updated = await shortUrlRepository.UpdateAsync(existingUrl, cancellationToken);
        if (!updated)
            throw new ServiceUnavailableException("Update Url");

        logger.LogInformation("Url updated successfully. ShortUrlId: {id}", id);
        return existingUrl.MapToShortUrlDto();
    }


    /// <inheritdoc />
    public async Task<bool> UpdateShortCodeAsync(long id, string newShortCode,
        CancellationToken cancellationToken = default)
    {
        if(await shortUrlRepository.IsShortCodeExistsAsync(newShortCode, cancellationToken))
            throw new ConflictException("Short code already exists.");
        
        var updated = await shortUrlRepository.UpdateShortCodeAsync(id, newShortCode, cancellationToken);
        if (!updated)
            throw new NotFoundException("Url", id);
        
        return updated;
    }

    
    /// <inheritdoc />
    public async Task<bool> DeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var deleted = await shortUrlRepository.DeleteByIdAsync(id, cancellationToken);
        if (!deleted)
            throw new NotFoundException("Url", id);

        logger.LogInformation("Url deleted. ID: {Id}.", id);
        return deleted;
    }


    /// <inheritdoc />
    public async Task<bool> DeleteByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var deleted = await shortUrlRepository.DeleteByShortCodeAsync(shortCode, cancellationToken);
        if (!deleted)
            throw new NotFoundException("Url", shortCode);

        logger.LogInformation("Url deleted. shortCode: {shortCode}.", shortCode);
        return deleted;
    }


    /// <inheritdoc />
    public async Task<bool> IsShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            throw new ArgumentException("Short code cannot be null or empty", nameof(shortCode));

        return await shortUrlRepository.IsShortCodeExistsAsync(shortCode, cancellationToken);
    }


    #region Create Short URL

    /// <summary>
    ///     Handles short URL creation for anonymous users.
    /// </summary>
    /// <param name="request">The request containing short URL creation details</param>
    /// <param name="authContext">The authentication context for the anonymous user</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="CreateShortUrlResponse" /> containing the created short URL information</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not anonymous</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    private async Task<CreateShortUrlResponse> HandleAnonymousCreationAsync(
        CreateShortUrlRequest request,
        IAuthenticationContext authContext,
        CancellationToken cancellationToken)
    {
        if (!authContext.IsAnonymous)
            throw new UnauthorizedAccessException("Only anonymous users can create with this method.");

        return await CreateShortUrlInternalAsync(request, authContext, cancellationToken);
    }


    /// <summary>
    ///     Handles short URL creation for authenticated users.
    /// </summary>
    /// <param name="request">The request containing short URL creation details</param>
    /// <param name="authContext">The authentication context for the user</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="CreateShortUrlResponse" /> containing the created short URL information</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated or user ID is missing</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    private async Task<CreateShortUrlResponse> HandleUserCreationAsync(
        CreateShortUrlRequest request,
        IAuthenticationContext authContext,
        CancellationToken cancellationToken)
    {
        if (!authContext.IsAuthenticated || authContext.UserId == null)
            throw new UnauthorizedAccessException("User must be logged in.");
        
        var canCreateMoreLinks =
            await userUsageService.CanCreateMoreLinksAsync(authContext.UserId.Value, cancellationToken);

        if (canCreateMoreLinks == false)
            throw new BusinessRuleException(
                "User has reached its limit for creating short URLs. Please upgrade your plan to create more short URLs.");
        
        return await CreateShortUrlInternalAsync(request, authContext, cancellationToken);
    }


    /// <summary>
    ///     Handles short URL creation for organization members.
    /// </summary>
    /// <param name="request">The request containing short URL creation details</param>
    /// <param name="authContext">The authentication context for the organization member</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="CreateShortUrlResponse" /> containing the created short URL information</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when organization member credentials are missing</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    private async Task<CreateShortUrlResponse> HandleOrganizationMemberCreationAsync(
        CreateShortUrlRequest request,
        IAuthenticationContext authContext,
        CancellationToken cancellationToken)
    {
        if (!authContext.IsAuthenticated || authContext.OrganizationId == null || authContext.MemberId == null)
            throw new UnauthorizedAccessException("Organization member credentials are required.");

        var canCreateMoreLinks =
            await organizationUsageService.CanCreateMoreLinksAsync(authContext.OrganizationId.Value, cancellationToken);

        if (canCreateMoreLinks == false)
            throw new BusinessRuleException("Organization has reached its limit for creating short URLs.");

        return await CreateShortUrlInternalAsync(request, authContext, cancellationToken);
    }


    /// <summary>
    ///     Internal method that performs the actual short URL creation logic.
    ///     Validates custom short codes, builds the entity, persists to database,
    ///     and generates short codes from ID if no custom code is provided.
    /// </summary>
    /// <param name="request">The request containing short URL creation details</param>
    /// <param name="authContext">The authentication context</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A <see cref="CreateShortUrlResponse" /> containing the created short URL information</returns>
    /// <exception cref="ConflictException">Thrown when a custom short code already exists</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    private async Task<CreateShortUrlResponse> CreateShortUrlInternalAsync(
        CreateShortUrlRequest request,
        IAuthenticationContext authContext,
        CancellationToken cancellationToken)
    {
        // Step 0: If custom code is provided, validate first
        if (!string.IsNullOrWhiteSpace(request.CustomShortCode))
            await ValidateCustomShortCodeAsync(request.CustomShortCode, cancellationToken);


        // Step 1: Build entity
        var shortUrl = new ShortUrl
        {
            OriginalUrl = request.OriginalUrl,
            ShortCode = request.CustomShortCode, // If null, we’ll set it later
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


        // Step 2: Insert into DB
        await shortUrlRepository.AddAsync(shortUrl, cancellationToken);

        // Step 3: If no custom short code, generate from ID
        if (string.IsNullOrWhiteSpace(shortUrl.ShortCode))
        {
            shortUrl.ShortCode = UrlCodeExtensions.GenerateCodeAsync(shortUrl.Id);
            await shortUrlRepository.UpdateShortCodeAsync(shortUrl.Id, shortUrl.ShortCode, cancellationToken);
        }

        logger.LogInformation("Short URL created successfully: {ShortCode} for {OwnerType}", shortUrl.ShortCode,
            authContext.OwnerType);
        return shortUrl.MapToCreateShortUrlResponse();
    }


    /// <summary>
    ///     Validates that a custom short code doesn't already exist in the system.
    /// </summary>
    /// <param name="customShortCode">The custom short code to validate</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <exception cref="ConflictException">Thrown when the custom short code already exists</exception>
    /// <exception cref="DatabaseException">Thrown when the database operation fails.</exception>
    private async Task ValidateCustomShortCodeAsync(string customShortCode,
        CancellationToken cancellationToken = default)
    {
        var isExist = await shortUrlRepository.IsShortCodeExistsAsync(customShortCode, cancellationToken);
        if (isExist)
            throw new ConflictException("Custom short code already exists.");
    }

    #endregion
}