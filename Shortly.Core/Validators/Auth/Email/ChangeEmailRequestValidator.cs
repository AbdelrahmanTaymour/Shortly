using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs.Email;

namespace Shortly.Core.Validators.Auth.Email;

public class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
{
    public ChangeEmailRequestValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .WithMessage("New email is required.")
            .EmailAddress()
            .WithMessage("Please provide a valid email address.")
            .MaximumLength(256)
            .WithMessage("Email address cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Current password is required.")
            .MinimumLength(1)
            .WithMessage("Password cannot be empty.");
    }
}
