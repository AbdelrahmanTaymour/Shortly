using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs.Password;
using Shortly.Core.Extensions;

namespace Shortly.Core.Validators.Auth.Password;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.")
            .MinimumLength(10)
            .WithMessage("Invalid token format.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128)
            .WithMessage("Password cannot exceed 128 characters.")
            .Must(ValidationExtensions.HasPasswordComplexity)
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required.")
            .Equal(x => x.NewPassword)
            .WithMessage("Password confirmation must match the new password.");
    }
}
