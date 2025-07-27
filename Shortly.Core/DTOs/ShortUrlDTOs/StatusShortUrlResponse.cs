namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record StatusShortUrlResponse(
    int Id,
    string OriginalUrl,
    string ShortCode,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int AccessCount);