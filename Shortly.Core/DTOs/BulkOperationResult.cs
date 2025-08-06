namespace Shortly.Core.DTOs;

public record BulkOperationResult(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount
);