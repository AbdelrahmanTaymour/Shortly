namespace Shortly.Core.DTOs.ShortUrlDTOs;

public record BulkCreateShortUrlResult(
    int TotalRequests,
    int ProcessedCount,
    int SuccessCount,
    int FailureCount,
    int ConflictCount,
    IReadOnlyCollection<string> ConflictMessages
);