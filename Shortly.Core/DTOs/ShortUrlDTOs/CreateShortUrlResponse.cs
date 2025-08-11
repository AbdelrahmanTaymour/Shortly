namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record CreateShortUrlResponse(
    long Id,
    string OriginalUrl,
    string ShortUrl,
    DateTime CreatedAt
    );