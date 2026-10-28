namespace Shortly.API.Controllers.ShortUrls;

public sealed record UpdateShortUrlRequest(string OriginalUrl, string? Title, string? Description);
