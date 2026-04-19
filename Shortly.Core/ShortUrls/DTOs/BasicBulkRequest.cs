namespace Shortly.Core.ShortUrls.DTOs;

public record BasicBulkRequest
{
    public IReadOnlyCollection<long> Ids { get; set; } = null!;
}