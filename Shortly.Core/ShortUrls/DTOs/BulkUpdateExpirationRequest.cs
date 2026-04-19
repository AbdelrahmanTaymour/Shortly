namespace Shortly.Core.ShortUrls.DTOs;

public record BulkUpdateExpirationRequest
{
    public IReadOnlyCollection<long> Ids { get; set; } = null!;
    public DateTime? NewExpirationDate { get; set; }
}