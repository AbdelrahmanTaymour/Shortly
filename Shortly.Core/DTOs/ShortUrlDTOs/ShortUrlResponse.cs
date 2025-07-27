namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record ShortUrlResponse(
    int Id,
    string OriginalUrl,
    string ShortCode,
    DateTime CreatedAt,
    DateTime UpdatedAt);