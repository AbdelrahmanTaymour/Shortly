namespace Shortly.API.Controllers.ShortUrls;

public sealed record BulkShortUrlIdsRequest(IReadOnlyCollection<long> ShortUrlIds);
