namespace Shortly.Api.Controllers.ShortUrls;

public sealed record BulkUpdateExpirationRequest(IReadOnlyCollection<long> ShortUrlIds, DateTime? ExpiresAt);
