namespace Shortly.Core.DTOs;

public record ShortUrlResponse(
    Guid Id,
    string OriginalUrl,
    string ShortCode,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    // Parameterless constructor
    public ShortUrlResponse(): this(Guid.Empty, String.Empty, 
        String.Empty, DateTime.MinValue, DateTime.MinValue) { }
}