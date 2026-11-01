using FluentValidation;

namespace Shortly.Core.ShortUrls.RedirectToShortUrl;

internal sealed class RedirectToShortUrlCommandValidator
    : AbstractValidator<RedirectToShortUrlCommand>
{
    public RedirectToShortUrlCommandValidator()
    {
        RuleFor(x => x.ShortCode)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Password)
            .MaximumLength(128);

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .MaximumLength(45)
            .Must(BeValidIpAddress)
            .WithMessage("A valid IP address is required.");

        RuleFor(x => x.SessionId)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.UserAgent)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(x => x.Referrer)
            .MaximumLength(2048);

        RuleFor(x => x.UtmSource).MaximumLength(255);
        RuleFor(x => x.UtmMedium).MaximumLength(255);
        RuleFor(x => x.UtmCampaign).MaximumLength(255);
        RuleFor(x => x.UtmTerm).MaximumLength(255);
        RuleFor(x => x.UtmContent).MaximumLength(255);
    }

    private static bool BeValidIpAddress(string value) =>
        System.Net.IPAddress.TryParse(value, out _);
}
