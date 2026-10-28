namespace Shortly.Domain.Abstractions;

public record BulkOperationResponse(
    int TotalProcessed,
    int SuccessCount,
    int FailureCount
);