using FluentValidation;
using Shortly.Core.DTOs.ShortUrlDTOs;

namespace Shortly.Core.Validators;

public class ShortUrlRequestValidator: AbstractValidator<ShortUrlRequest>
{
    public ShortUrlRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty().WithMessage("Original URL is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("A valid URL is required.");

    }
}