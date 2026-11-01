using FluentValidation;

namespace Shortly.Core.Account.GoogleLogin;

internal sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.GoogleId).NotEmpty();
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Name)
            .MaximumLength(150)
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Picture)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Picture));
    }
}