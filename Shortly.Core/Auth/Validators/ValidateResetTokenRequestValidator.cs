using FluentValidation;
using Shortly.Core.Auth.DTOs.Email;

namespace Shortly.Core.Auth.Validators;

public class ValidateResetTokenRequestValidator : AbstractValidator<ValidateResetTokenRequest>
{
    public ValidateResetTokenRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.")
            .MinimumLength(10)
            .WithMessage("Invalid token format.");
    }
}
