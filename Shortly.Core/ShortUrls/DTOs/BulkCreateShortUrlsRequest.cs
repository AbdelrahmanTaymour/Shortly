namespace Shortly.Core.ShortUrls.DTOs;

public record BulkCreateShortUrlsRequest
{
    public IReadOnlyCollection<CreateShortUrlRequest> Requests { get; set; } = [];
}