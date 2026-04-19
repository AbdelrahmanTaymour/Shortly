using FluentValidation;
using Shortly.Core.ShortUrls.DTOs;

namespace Shortly.Core.ShortUrls.Validators;

public class UpdateShortCodeRequestValidator : AbstractValidator<UpdateShortCodeRequest>
{
    public UpdateShortCodeRequestValidator()
    {
        RuleFor(x => x.NewShortCode)
            .NotEmpty()
            .WithMessage("NewShortCode is required.")
            .MaximumLength(15)
            .WithMessage("Custom short code cannot exceed 15 characters.")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Custom short code can only contain letters, numbers, hyphens, and underscores.");
    }
}