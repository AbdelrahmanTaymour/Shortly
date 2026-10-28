using Shortly.Domain.Abstractions;

namespace Shortly.Domain.ShortUrls;

public sealed record OriginalUrl
{
    private const int MaxLength = 2048;

    public string Value { get; }

    private OriginalUrl(string value)
    {
        Value = value;
    }

    public static Result<OriginalUrl> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<OriginalUrl>(ShortUrlErrors.OriginalUrlEmpty);

        if (value.Length > MaxLength)
            return Result.Failure<OriginalUrl>(ShortUrlErrors.OriginalUrlTooLong);

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure<OriginalUrl>(ShortUrlErrors.OriginalUrlInvalid);
        }

        return new OriginalUrl(uri.ToString());
    }

    public override string ToString() => Value;
}