using FluentValidation;

namespace Shortly.Core.ShortUrls.BulkCreateShortUrls;

internal sealed class BulkCreateShortUrlItemValidator
    : AbstractValidator<BulkCreateShortUrlItem>
{
    public BulkCreateShortUrlItemValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeValidUrl)
            .WithMessage("Original URL must be a valid HTTP or HTTPS URL.");

        RuleFor(x => x.Title)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }

    private static bool BeValidUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
