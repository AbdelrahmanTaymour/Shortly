using FluentValidation;

namespace Shortly.Core.ShortUrls.BulkUpdateExpiration;

internal sealed class BulkUpdateExpirationCommandValidator
    : AbstractValidator<BulkUpdateExpirationCommand>
{
    public BulkUpdateExpirationCommandValidator()
    {
        RuleFor(x => x.ShortUrlIds)
            .NotEmpty()
            .Must(ids => ids.Count <= 100)
            .WithMessage("You can process a maximum of 100 short URLs at once.");

        RuleForEach(x => x.ShortUrlIds).GreaterThan(0);

        RuleFor(x => x.ShortUrlIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate short URL IDs are not allowed.");

        RuleFor(x => x.ExpiresAt)
            .Must(BeInFuture)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future.");
    }

    private static bool BeInFuture(DateTime? value) =>
        value > DateTime.UtcNow;
}
