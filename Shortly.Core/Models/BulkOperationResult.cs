namespace Shortly.Core.Models;

public record BulkOperationResult(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount
);