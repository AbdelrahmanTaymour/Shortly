namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record DuplicatesUrlsResponse(string OriginalUrl, IReadOnlyCollection<ShortUrlDto> Duplicates);