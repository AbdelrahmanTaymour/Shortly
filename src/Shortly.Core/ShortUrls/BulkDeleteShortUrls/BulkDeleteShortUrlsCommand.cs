using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.ShortUrls.BulkDeleteShortUrls;

public sealed record BulkDeleteShortUrlsCommand(IReadOnlyCollection<long> ShortUrlIds) : ICommand<BulkOperationResponse>;