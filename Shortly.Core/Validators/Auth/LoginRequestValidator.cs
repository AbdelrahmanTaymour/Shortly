using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs;

namespace Shortly.Core.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Original URL is required.")
            .EmailAddress().WithMessage("Invalid email address format.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}