namespace Shortly.Core.ShortUrls.DTOs;

public record UrlRedirectResult(string OriginalUrl, bool RequiresPassword);