namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record BulkCreateShortUrlsRequest
{
    public IReadOnlyCollection<CreateShortUrlRequest> Requests { get; set; } = [];
}