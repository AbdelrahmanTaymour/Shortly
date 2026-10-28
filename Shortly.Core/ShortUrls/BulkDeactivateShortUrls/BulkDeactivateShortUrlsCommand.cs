using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.BulkDeactivateShortUrls;

public sealed record BulkDeactivateShortUrlsCommand(IReadOnlyCollection<long> ShortUrlIds) : ICommand<BulkOperationResponse>;