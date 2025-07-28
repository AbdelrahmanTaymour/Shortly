using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;
using Shortly.Core.Extensions;
using Shortly.Core.Mappers;

namespace Shortly.Core.Services;

internal class ShortUrlsService(IShortUrlRepository shortUrlRepository) : IShortUrlsService
{
    private readonly IShortUrlRepository _shortUrlRepository = shortUrlRepository;

    public async Task<IEnumerable<ShortUrlResponse>> GetAllAsync()
    {
        var allShortedUrls = await _shortUrlRepository.GetAllAsync();
        return allShortedUrls.MapToShortUrlResponseList();
    }

    public async Task<ShortUrlResponse?> GetByShortCodeAsync(string shortCode)
    {
        var shortUrl = await _shortUrlRepository.GetShortUrlByShortCodeAsync(shortCode);
        if (shortUrl is not null)
        {
            //TODO: Apply Event-Based Tracking for
            // increment the access count in the database
            await _shortUrlRepository.IncrementAccessCountAsync(shortCode);

        }
        return shortUrl?.MapToShortUrlResponse();
    }

    public async Task<ShortUrlResponse> CreateShortUrlAsync(ShortUrlRequest shortUrlRequest)
    {
        var shortUrlDomain = shortUrlRequest.MapToShortUrl();
        
        // Check if a custom short code is provided
        if (!string.IsNullOrEmpty(shortUrlRequest.CustomShortCode))
        {
            // Validate custom short code
            var (isValid, errorMessage) = UrlCodeExtensions.ValidateCustomCode(shortUrlRequest.CustomShortCode);
            if (!isValid)
                throw new ArgumentException($"Invalid custom short code: {errorMessage}");
                
            // Check if custom code already exists
            if (await _shortUrlRepository.ShortCodeExistsAsync(shortUrlRequest.CustomShortCode))
                throw new ArgumentException("Custom short code already exists");
                
            shortUrlDomain.ShortCode = shortUrlRequest.CustomShortCode;
            
            // Create entity
            shortUrlDomain = await _shortUrlRepository.CreateShortUrlAsync(shortUrlDomain);
            if (shortUrlDomain is null)
                throw new Exception("Error creating short url");
        }
        else
        {
            // Create an entity first to get ID
            shortUrlDomain = await _shortUrlRepository.CreateShortUrlAsync(shortUrlDomain);
            if (shortUrlDomain is null)
                throw new Exception("Error creating short url");
            
            // Generate optimized short code 
            var shortCode = await UrlCodeExtensions.GenerateCodeAsync(
                shortUrlDomain.Id, 
                _shortUrlRepository,
                await GetOptimalCodeLength()
            );
            
            // Update the entity with the generated/custom short code
            shortUrlDomain.ShortCode = shortCode;
            await _shortUrlRepository.SaveChangesAsync();
        }
        
        
        return shortUrlDomain.MapToShortUrlResponse();
    }

    public async Task<ShortUrlResponse?> UpdateShortUrlAsync(string shortCode, ShortUrlRequest updatedShortUrlRequest)
    {
        var existingUrl = await _shortUrlRepository.GetShortUrlByShortCodeAsync(shortCode);
        if (existingUrl is null)
        {
            return null;
        }
        
        existingUrl.OriginalUrl = updatedShortUrlRequest.OriginalUrl;
        existingUrl.UpdatedAt = DateTime.UtcNow;
        
        await _shortUrlRepository.SaveChangesAsync();
        
        return existingUrl.MapToShortUrlResponse();
    }

    public async Task<bool> DeleteShortUrlAsync(string shortCode)
    {
        var deletedUrl = await _shortUrlRepository.DeleteByShortCodeAsync(shortCode);
        return deletedUrl is not null;
    }

    public async Task<StatusShortUrlResponse?> GetStatisticsAsync(string shortCode)
    {
        var existingUrl = await _shortUrlRepository.GetShortUrlByShortCodeAsync(shortCode);
        if (existingUrl is null)
        {
            return null;
        }

        return existingUrl.MapToStatusShortUrlResponse();
    }
    
    /// <summary>
    /// Determines optimal code length based on current URL count and growth projections
    /// </summary>
    private async Task<int> GetOptimalCodeLength()
    {
        var projectedUrls = await _shortUrlRepository.GetShortUrlCountAsync();
        double maxCollisionProbability = UrlCodeExtensions.GetCollisionProbability(6, projectedUrls);
        
        return UrlCodeExtensions.RecommendCodeLength(projectedUrls, maxCollisionProbability);
    }
}