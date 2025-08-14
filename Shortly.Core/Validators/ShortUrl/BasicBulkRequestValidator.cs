using FluentValidation;
using Shortly.Core.DTOs.ShortUrlDTOs;

namespace Shortly.Core.Validators.ShortUrl;

public class BasicBulkRequestValidator : AbstractValidator<BasicBulkRequest>
{
    public BasicBulkRequestValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull()
            .WithMessage("Requests collection cannot be null.")
            .NotEmpty()
            .WithMessage("Requests collection cannot be empty.")
            .Must(x => x.Count <= 1000)
            .WithMessage("Cannot process more than 1000 URLs in a single bulk operation.");
    }
}