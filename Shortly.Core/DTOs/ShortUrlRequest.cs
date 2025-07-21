namespace Shortly.Core.DTOs;

public record ShortUrlRequest(string OriginalUrl, string? CustomShortCode = null)
{
    // Parameterless constructor
    public ShortUrlRequest(): this(String.Empty) { }
}