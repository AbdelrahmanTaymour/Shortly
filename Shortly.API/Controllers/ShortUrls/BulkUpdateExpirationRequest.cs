namespace Shortly.API.Controllers.ShortUrls;

public sealed record BulkUpdateExpirationRequest(IReadOnlyCollection<long> ShortUrlIds, DateTime? ExpiresAt);
