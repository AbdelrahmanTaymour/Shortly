using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.Security;

namespace Shortly.Core.Validators.Users;

public class UpdateUserSecurityDtoValidator : AbstractValidator<UpdateUserSecurityDto>
{
    public UpdateUserSecurityDtoValidator()
    {
        RuleFor(x => x.FailedLoginAttempts)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Failed login attempts cannot be negative")
            .LessThan(10)
            .WithMessage("Failed login attempts seems unreasonably high");

        When(x => x.LockedUntil.HasValue, () =>
        {
            RuleFor(x => x.LockedUntil)
                .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
                .WithMessage("Lock expiration should not be too far in the past");
        });

        When(x => x.TwoFactorEnabled, () =>
        {
            RuleFor(x => x.TwoFactorSecret)
                .NotEmpty()
                .WithMessage("Two-factor secret is required when two-factor authentication is enabled")
                .Length(32, 64)
                .WithMessage("Two-factor secret must be between 32 and 64 characters");
        });

        When(x => !x.TwoFactorEnabled, () =>
        {
            RuleFor(x => x.TwoFactorSecret)
                .Null()
                .WithMessage("Two-factor secret must be null when two-factor authentication is disabled");
        });

        RuleFor(x => x.PasswordResetToken)
            .Length(32, 256)
            .WithMessage("Password reset token must be between 32 and 256 characters")
            .When(x => !string.IsNullOrEmpty(x.PasswordResetToken));

        When(x => !string.IsNullOrEmpty(x.PasswordResetToken), () =>
        {
            RuleFor(x => x.TokenExpiresAt)
                .NotNull()
                .WithMessage("Token expiration date is required when password reset token is present")
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Password reset token should not be expired");
        });

        When(x => string.IsNullOrEmpty(x.PasswordResetToken), () =>
        {
            RuleFor(x => x.TokenExpiresAt)
                .Null()
                .WithMessage("Token expiration date must be null when no password reset token is present");
        });
    }
}