using AutoMapper;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;
using Shortly.Core.Utilities;

namespace Shortly.Core.Services;

internal class ShortUrlsService(IShortUrlRepository shortUrlRepository, IMapper mapper) : IShortUrlsService
{
    private readonly IShortUrlRepository _shortUrlRepository = shortUrlRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<List<ShortUrlResponse>> GetAllAsync()
    {
        var allShortedUrls = await _shortUrlRepository.GetAllAsync();
        return _mapper.Map<List<ShortUrlResponse>>(allShortedUrls);
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
        return _mapper.Map<ShortUrlResponse>(shortUrl);
    }

    public async Task<ShortUrlResponse> CreateShortUrlAsync(ShortUrlRequest shortUrlRequest)
    {
        var shortUrlDomain = _mapper.Map<ShortUrl>(shortUrlRequest);
        
        // Enhanced URL generation algorithm with multiple strategies
        string shortCode;
        
        // Check if custom short code is provided
        if (!string.IsNullOrEmpty(shortUrlRequest.CustomShortCode))
        {
            // Validate custom short code
            var (isValid, errorMessage) = EnhancedUrlCodeGenerator.ValidateCustomCode(shortUrlRequest.CustomShortCode);
            if (!isValid)
                throw new ArgumentException($"Invalid custom short code: {errorMessage}");
                
            // Check if custom code already exists
            if (await _shortUrlRepository.ShortCodeExistsAsync(shortUrlRequest.CustomShortCode))
                throw new ArgumentException("Custom short code already exists");
                
            shortCode = shortUrlRequest.CustomShortCode;
        }
        else
        {
            // Create entity first to get ID
            shortUrlDomain = await _shortUrlRepository.CreateShortUrlAsync(shortUrlDomain);
            if (shortUrlDomain is null)
                throw new Exception("Error creating short url");
            
            // Generate optimized short code using the new algorithm
            shortCode = await EnhancedUrlCodeGenerator.GenerateCodeAsync(
                shortUrlDomain.Id, 
                _shortUrlRepository, 
                GetOptimalCodeLength()
            );
        }
        
        // Update the entity with the generated/custom short code
        shortUrlDomain.ShortCode = shortCode;
        await _shortUrlRepository.SaveChangesAsync();
        
        return _mapper.Map<ShortUrlResponse>(shortUrlDomain);
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
        
        return _mapper.Map<ShortUrlResponse>(existingUrl);
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

        return _mapper.Map<StatusShortUrlResponse>(existingUrl);
    }
    
    /// <summary>
    /// Determines optimal code length based on current URL count and growth projections
    /// </summary>
    private int GetOptimalCodeLength()
    {
        // For now, use a conservative approach
        // In production, you could query the database for current URL count
        // and use EnhancedUrlCodeGenerator.RecommendCodeLength()
        
        const long projectedUrls = 10_000_000; // 10 million URLs projection
        const double maxCollisionProbability = 0.001; // 0.1% collision rate
        
        return EnhancedUrlCodeGenerator.RecommendCodeLength(projectedUrls, maxCollisionProbability);
    }
}