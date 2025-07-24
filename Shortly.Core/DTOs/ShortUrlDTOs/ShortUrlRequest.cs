namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record ShortUrlRequest(string OriginalUrl, string? CustomShortCode = null)
{
    // Parameterless constructor
    public ShortUrlRequest(): this(String.Empty) { }
}