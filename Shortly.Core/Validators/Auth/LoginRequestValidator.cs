using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Extensions;

namespace Shortly.Core.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.EmailOrUsername)
            .NotEmpty()
            .WithMessage("Email or username is required")
            .MaximumLength(254)
            .WithMessage("Email or username cannot exceed 254 characters")
            .Must(BeValidEmailOrUsername)
            .WithMessage(
                "Must be a valid email address or username (3-50 characters, letters, numbers, hyphens, and underscores only)");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }

    private bool BeValidEmailOrUsername(string emailOrUsername)
    {
        if (string.IsNullOrWhiteSpace(emailOrUsername))
            return false;

        // Check if it's a valid email
        if (IsValidEmail(emailOrUsername))
            return true;

        // Check if it's a valid username
        return IsValidUsername(emailOrUsername);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidUsername(string username)
    {
        // Username rules: 3-50 characters, letters, numbers, hyphens, and underscores only
        return username.Length >= 3 &&
               username.Length <= 50 &&
               System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$");
    }
}