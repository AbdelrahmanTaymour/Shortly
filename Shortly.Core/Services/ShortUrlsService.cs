using AutoMapper;
using Shortly.Core.DTOs;
using Shortly.Core.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;

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
        
        //TODO: ADD THE SHORTEN CODE LOGIC HERE.
        shortUrlDomain.ShortCode = Guid.NewGuid().ToString().Substring(0, 6);
        
        shortUrlDomain = await _shortUrlRepository.CreateShortUrlAsync(shortUrlDomain);
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
}