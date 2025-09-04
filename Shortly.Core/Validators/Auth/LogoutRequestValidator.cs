using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs;
using Shortly.Core.Extensions;

namespace Shortly.Core.Validators.Auth;

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    /// <summary>
    /// Initializes validation rules for logout requests.
    /// </summary>
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required for logout.")
            .NotNull()
            .WithMessage("Refresh token cannot be null.")
            .MinimumLength(20)
            .WithMessage("Refresh token appears to be invalid (too short).")
            .MaximumLength(500)
            .WithMessage("Refresh token appears to be invalid (too long).")
            .Must(ValidationExtensions.BeValidBase64String)
            .WithMessage("Refresh token must be a valid base64-encoded string.")
            .Must(ValidationExtensions.BeNotContainWhitespace)
            .WithMessage("Refresh token cannot contain whitespace characters.");
    }
}
