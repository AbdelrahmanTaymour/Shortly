using System.Diagnostics;
using Shortly.Core.DTOs.ShortUrlDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class ShortUrlMapper
{
    public static ShortUrlResponse MapToShortUrlResponse(this ShortUrl shortUrl)
    {
        return new ShortUrlResponse
        (
            shortUrl.Id,
            shortUrl.OriginalUrl,
            shortUrl.ShortCode ?? throw new InvalidOperationException(),
            shortUrl.CreatedAt,
            shortUrl.UpdatedAt
        );
    }

    public static IEnumerable<ShortUrlResponse> MapToShortUrlResponseList(this IEnumerable<ShortUrl> shortUrls)
    {
        return shortUrls.Select(MapToShortUrlResponse);
    }

    public static ShortUrl MapToShortUrl(this ShortUrlRequest shortUrlRequest)
    {
        return new ShortUrl
        {
            OriginalUrl = shortUrlRequest.OriginalUrl,
            ShortCode = shortUrlRequest.CustomShortCode,
        };
    }

    public static StatusShortUrlResponse MapToStatusShortUrlResponse(this ShortUrl shortUrl)
    {
        return new StatusShortUrlResponse(
            shortUrl.Id,
            shortUrl.OriginalUrl,
            shortUrl.ShortCode,
            shortUrl.CreatedAt,
            shortUrl.UpdatedAt,
            shortUrl.AccessCount
        );
    }
}