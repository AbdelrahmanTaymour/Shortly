namespace Shortly.Core.ShortUrls.DTOs;

public record DuplicatesUrlsResponse(string OriginalUrl, IReadOnlyCollection<ShortUrlDto> Duplicates);