namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record BasicBulkRequest
{
    public IReadOnlyCollection<long> Ids { get; set; } = null!;
}