using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs;

namespace Shortly.Core.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.EmailOrUsername)
            .NotEmpty().WithMessage("Email or username is required.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}