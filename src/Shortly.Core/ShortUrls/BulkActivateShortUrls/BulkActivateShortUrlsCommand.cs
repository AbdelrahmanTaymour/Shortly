using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.BulkActivateShortUrls;

public sealed record BulkActivateShortUrlsCommand(IReadOnlyCollection<long> ShortUrlIds) : ICommand<BulkOperationResponse>;