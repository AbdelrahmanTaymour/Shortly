using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

public sealed record BulkCreateShortUrlsCommand(IReadOnlyCollection<BulkCreateShortUrlItem> Urls)
    : ICommand<BulkCreateShortUrlsResponse>;
