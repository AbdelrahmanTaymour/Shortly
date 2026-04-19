namespace Shortly.Core.ShortUrls.DTOs;

public record BulkCreateShortUrlResult(
    int TotalRequests,
    int ProcessedCount,
    int SuccessCount,
    int FailureCount,
    int ConflictCount,
    IReadOnlyCollection<string> ConflictMessages
);