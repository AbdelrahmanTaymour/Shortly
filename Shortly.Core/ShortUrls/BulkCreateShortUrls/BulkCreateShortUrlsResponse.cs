namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

public class BulkCreateShortUrlsResponse(
    int TotalProcessed, int SuccessCount, int FailureCount,
    IReadOnlyList<long> CreatedIds, IReadOnlyList<string> Errors);