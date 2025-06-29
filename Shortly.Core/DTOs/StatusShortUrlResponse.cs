namespace Shortly.Core.DTOs;

public record StatusShortUrlResponse(
    Guid Id,
    string OriginalUrl,
    string ShortCode,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int AccessCount)
{
    // Parameterless constructor
    public StatusShortUrlResponse(): this(Guid.Empty, String.Empty, 
        String.Empty, DateTime.MinValue, DateTime.MinValue, 0) { }
}