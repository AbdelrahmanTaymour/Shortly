using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs.Email;

namespace Shortly.Core.Validators.Auth.Email;

public class ConfirmEmailChangeRequestValidator : AbstractValidator<ConfirmEmailChangeRequest>
{
    public ConfirmEmailChangeRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.")
            .MinimumLength(10)
            .WithMessage("Invalid token format.");
    }
}
