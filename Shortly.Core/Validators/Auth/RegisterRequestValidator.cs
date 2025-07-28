using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.RepositoryContract;

namespace Shortly.Core.Validators.Auth;

public class RegisterRequestValidator: AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(IUserRepository userRepository)
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Name is required.");

        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address format.");

        RuleFor(request => request.Username)
            .NotEmpty().WithMessage("Username is required.");
        
        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}