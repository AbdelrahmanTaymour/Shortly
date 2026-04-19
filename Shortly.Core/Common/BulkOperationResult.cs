namespace Shortly.Core.Common;

public record BulkOperationResult(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount
);