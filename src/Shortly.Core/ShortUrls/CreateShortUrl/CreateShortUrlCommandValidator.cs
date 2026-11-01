using FluentValidation;

namespace Shortly.Core.ShortUrls.CreateShortUrl;

internal sealed class CreateShortUrlCommandValidator
    : AbstractValidator<CreateShortUrlCommand>
{
    public CreateShortUrlCommandValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeValidUrl)
            .WithMessage("Original URL must be a valid HTTP or HTTPS URL.");

        RuleFor(x => x.CustomShortCode)
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomShortCode))
            .WithMessage("Short code can only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.ClickLimit)
            .GreaterThan(0)
            .When(x => x.ClickLimit.HasValue);

        RuleFor(x => x.ExpiresAt)
            .Must(BeInFuture)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future.");

        RuleFor(x => x.Title)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }

    private static bool BeValidUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool BeInFuture(DateTime? value) =>
        value > DateTime.UtcNow;
}
