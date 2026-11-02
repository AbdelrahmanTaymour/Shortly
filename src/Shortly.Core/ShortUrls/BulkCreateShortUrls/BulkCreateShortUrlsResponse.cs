namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

public sealed record BulkCreateShortUrlsResponse(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<long> CreatedIds,
    IReadOnlyList<string> Errors);