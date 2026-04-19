namespace Shortly.Core.ShortUrls.DTOs;

public record CreateShortUrlResponse(
    long Id,
    string OriginalUrl,
    string ShortUrl,
    DateTime CreatedAt
    );