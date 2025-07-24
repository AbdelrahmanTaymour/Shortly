namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record ShortUrlResponse(
    int Id,
    string OriginalUrl,
    string ShortCode,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    // Parameterless constructor
    public ShortUrlResponse(): this(-1, String.Empty, 
        String.Empty, DateTime.MinValue, DateTime.MinValue) { }
}