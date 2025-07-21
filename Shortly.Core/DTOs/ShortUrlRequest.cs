namespace Shortly.Core.DTOs;

public record ShortUrlRequest(
    string OriginalUrl,
    string? CustomShortCode = null,
    DateTime? ExpirationDate = null,
    string? Password = null,
    string? Title = null,
    string? Description = null,
    List<string>? Tags = null,
    bool IsPrivate = false
)
{
    // Parameterless constructor
    public ShortUrlRequest(): this(String.Empty) { }
}