using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs.Password;

namespace Shortly.Core.Validators.Auth.Password;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Please provide a valid email address.")
            .MaximumLength(256)
            .WithMessage("Email address cannot exceed 256 characters.");
    }
}
