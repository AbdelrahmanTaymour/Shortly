using FluentValidation;
using Shortly.Domain.ShortUrls;

namespace Shortly.Core.ShortUrls.UpdateShortUrl;

public sealed class UpdateShortUrlCommandValidator : AbstractValidator<UpdateShortUrlCommand>
{
    public UpdateShortUrlCommandValidator()
    {
        RuleFor(c => c.OriginalUrl).NotEmpty().MaximumLength(2048);

        RuleFor(c => c.ShortCode)
            .NotEmpty()
            .Length(ShortCode.MinLength, ShortCode.MaxLength)
            .Matches("^[a-zA-Z0-9_-]+$");

        RuleFor(c => c.Title).MaximumLength(50);
        RuleFor(c => c.Description).MaximumLength(500);

        RuleFor(c => c.ClickLimit)
            .Must(limit => limit == -1 || limit > 0)
            .WithMessage("Click limit must be -1 (unlimited) or greater than zero.");

        RuleFor(c => c.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .When(c => c.ExpiresAt.HasValue);
    }
}