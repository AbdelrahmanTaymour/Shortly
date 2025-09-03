using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs.Email;

namespace Shortly.Core.Validators.Auth;

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
