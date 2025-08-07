using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs;

namespace Shortly.Core.Validators.Auth;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MinimumLength(44).WithMessage("Refresh token is too short.")
            .Must(BeValidBase64).WithMessage("Invalid refresh token format.");
    }

    private bool BeValidBase64(string token)
    {
        var bytes = Convert.FromBase64String(token);
        return bytes.Length == 32;
    }
}