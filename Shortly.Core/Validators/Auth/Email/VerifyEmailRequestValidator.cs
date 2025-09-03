using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs.Email;

namespace Shortly.Core.Validators.Auth.Email;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.")
            .MinimumLength(10)
            .WithMessage("Invalid token format.");
    }
}
