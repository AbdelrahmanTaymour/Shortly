namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record BulkUpdateExpirationRequest
{
    public IReadOnlyCollection<long> Ids { get; set; } = null!;
    public DateTime? NewExpirationDate { get; set; }
}