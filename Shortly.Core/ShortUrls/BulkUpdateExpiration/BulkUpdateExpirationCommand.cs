using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.BulkUpdateExpiration;

public sealed record BulkUpdateExpirationCommand(IReadOnlyCollection<long> ShortUrlIds, DateTime? ExpiresAt)
    : ICommand<BulkOperationResponse>;
