namespace Shortly.Api.Controllers.ShortUrls;

public sealed record BulkShortUrlIdsRequest(IReadOnlyCollection<long> ShortUrlIds);
