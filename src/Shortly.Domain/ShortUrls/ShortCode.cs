using System.Text.RegularExpressions;
using Shortly.Domain.Abstractions;

namespace Shortly.Domain.ShortUrls;

public sealed partial record ShortCode
{
    public const int MinLength = 4;
    public const int MaxLength = 20;

    public string Value { get; }

    private ShortCode(string value)
    {
        Value = value;
    }

    public static Result<ShortCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeEmpty);

        if (value.Length is < MinLength or > MaxLength)
            return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeInvalidLength);

        if (!ValidCharacters().IsMatch(value))
            return Result.Failure<ShortCode>(ShortUrlErrors.ShortCodeInvalidCharacters);

        return new ShortCode(value);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex ValidCharacters();
}