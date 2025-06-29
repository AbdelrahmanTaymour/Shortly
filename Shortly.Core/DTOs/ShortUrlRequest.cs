namespace Shortly.Core.DTOs;

public record ShortUrlRequest(string OriginalUrl)
{
    // Parameterless constructor
    public ShortUrlRequest(): this(String.Empty) { }
}