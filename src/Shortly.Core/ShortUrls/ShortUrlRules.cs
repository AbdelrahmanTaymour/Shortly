using FluentValidation;
using Shortly.Core.Abstractions.Clock;

namespace Shortly.Core.ShortUrls;

/// <summary>Reusable FluentValidation extension rules shared across short URL commands and items.</summary>
internal static class ShortUrlRules
{
    /// <summary>Validates that a string is a well-formed absolute HTTP or HTTPS URL.</summary>
    public static IRuleBuilderOptions<T, string> MustBeHttpUrl<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Original URL must be a valid HTTP or HTTPS URL.");

    /// <summary>Validates standard short code formatting (alphanumeric, hyphens, and underscores).</summary>
    public static IRuleBuilderOptions<T, string?> MustBeShortCodeFormat<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Short code can only contain letters, numbers, hyphens, and underscores.");

    /// <summary>Validates that an optional date falls strictly in the future using the application clock.</summary>
    public static IRuleBuilderOptions<T, DateTime?> MustBeInTheFuture<T>(this IRuleBuilder<T, DateTime?> rule, IDateTimeProvider clock) =>
        rule.Must(value => value > clock.UtcNow)
            .WithMessage("Expiration date must be in the future.");
}