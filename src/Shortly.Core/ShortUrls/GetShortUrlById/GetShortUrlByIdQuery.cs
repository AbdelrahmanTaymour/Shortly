using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.ShortUrls.GetShortUrlById;

public sealed record GetShortUrlByIdQuery(long ShortUrlId) : IQuery<ShortUrlResponse>;
