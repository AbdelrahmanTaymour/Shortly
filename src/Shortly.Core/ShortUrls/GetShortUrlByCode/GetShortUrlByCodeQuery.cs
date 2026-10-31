using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.ShortUrls.GetShortUrlById;

namespace Shortly.Core.ShortUrls.GetShortUrlByCode;

public sealed record GetShortUrlByCodeQuery(string ShortCode) : IQuery<ShortUrlResponse>;
