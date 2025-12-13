using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Extensions;
using Shortly.Core.RepositoryContract.UserManagement;

namespace Shortly.Core.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
            .Must(username => username.IsValidUsername())
            .WithMessage("Username can only contain letters, numbers, underscores, hyphens, and periods.")
            .Must(username => !username.StartsWith('.') && !username.EndsWith('.'))
            .WithMessage("Username cannot start or end with a period.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .Length(8, 128).WithMessage("Password must be between 8 and 128 characters.")
            .Must(password => password.HasPasswordComplexity()).WithMessage(
                "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");
    }
}