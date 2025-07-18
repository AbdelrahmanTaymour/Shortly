namespace Shortly.Core.DTOs;

public record StatusShortUrlResponse(
    int Id,
    string OriginalUrl,
    string ShortCode,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int AccessCount)
{
    // Parameterless constructor
    public StatusShortUrlResponse(): this(-1, String.Empty, 
        String.Empty, DateTime.MinValue, DateTime.MinValue, 0) { }
}