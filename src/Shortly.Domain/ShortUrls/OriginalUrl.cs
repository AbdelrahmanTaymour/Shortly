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

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure<OriginalUrl>(ShortUrlErrors.OriginalUrlInvalid);
        }

        // The normalized form is what gets stored and compared, so that is what the length limit applies to.
        var normalized = uri.ToString();
        if (normalized.Length > MaxLength)
            return Result.Failure<OriginalUrl>(ShortUrlErrors.OriginalUrlTooLong);

        return new OriginalUrl(normalized);
    }

    public override string ToString() => Value;
}