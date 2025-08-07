using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

/// <summary>
/// Provides extension methods for mapping between ShortUrl domain entities and their corresponding DTOs.
/// </summary>
/*public static class ShortUrlMapper
{
    /// <summary>
    /// Maps a ShortUrl domain entity to a ShortUrlResponse DTO.
    /// </summary>
    /// <param name="shortUrl">The ShortUrl entity to map.</param>
    /// <returns>A ShortUrlResponse containing the mapped data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ShortUrl's ShortCode is null.</exception>
    public static ShortUrlResponse MapToShortUrlResponse(this ShortUrl shortUrl)
    {
        if (shortUrl.ShortCode == null)
            throw new InvalidOperationException($"ShortUrl with ID '{shortUrl.Id}' cannot be mapped because the ShortCode is null.");

        return new ShortUrlResponse
        (
            shortUrl.Id,
            shortUrl.OriginalUrl,
            shortUrl.ShortCode,
            shortUrl.CreatedAt,
            shortUrl.UpdatedAt
        );
    }

    /// <summary>
    /// Maps a collection of ShortUrl entities to a collection of ShortUrlResponse DTOs.
    /// </summary>
    /// <param name="shortUrls">The collection of ShortUrl entities to map.</param>
    /// <returns>An IEnumerable of ShortUrlResponse DTOs.</returns>
    public static IEnumerable<ShortUrlResponse> MapToShortUrlResponseList(this IEnumerable<ShortUrl> shortUrls)
    {
        return shortUrls.Select(MapToShortUrlResponse);
    }


    /// <summary>
    /// Maps a ShortUrlRequest DTO to a ShortUrl domain entity.
    /// </summary>
    /// <param name="shortUrlRequest">The ShortUrlRequest DTO to map.</param>
    /// <returns>A new ShortUrl entity with the mapped data.</returns>
    public static ShortUrl MapToShortUrl(this ShortUrlRequest shortUrlRequest)
    {
        return new ShortUrl
        {
            OriginalUrl = shortUrlRequest.OriginalUrl,
            ShortCode = shortUrlRequest.CustomShortCode,
        };
    }

    /// <summary>
    /// Maps a ShortUrl domain entity to a StatusShortUrlResponse DTO, including usage statistics.
    /// </summary>
    /// <param name="shortUrl">The ShortUrl entity to map.</param>
    /// <returns>A StatusShortUrlResponse containing the mapped data and usage statistics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ShortUrl's ShortCode is null.</exception>
    public static StatusShortUrlResponse MapToStatusShortUrlResponse(this ShortUrl shortUrl)
    {
        if (shortUrl.ShortCode == null)
            throw new InvalidOperationException($"ShortUrl with ID '{shortUrl.Id}' cannot be mapped because the ShortCode is null.");

        return new StatusShortUrlResponse(
            shortUrl.Id,
            shortUrl.OriginalUrl,
            shortUrl.ShortCode,
            shortUrl.CreatedAt,
            shortUrl.UpdatedAt,
            shortUrl.TotalClicks
        );
    }
}*/