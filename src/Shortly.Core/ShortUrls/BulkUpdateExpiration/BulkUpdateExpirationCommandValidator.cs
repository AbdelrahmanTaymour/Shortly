using FluentValidation;
using Shortly.Core.Abstractions.Clock;

namespace Shortly.Core.ShortUrls.BulkUpdateExpiration;

internal sealed class BulkUpdateExpirationCommandValidator
    : AbstractValidator<BulkUpdateExpirationCommand>
{
    public BulkUpdateExpirationCommandValidator(IDateTimeProvider clock)
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
            .MustBeInTheFuture(clock)
            .When(x => x.ExpiresAt.HasValue);
    }
}
