using FluentValidation;
using Shortly.Core.DTOs.UsersDTOs.Security;

namespace Shortly.Core.Validators.Users;

public class UserSecurityDtoValidator : AbstractValidator<UserSecurityDto>
{
    public UserSecurityDtoValidator()
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
        

        RuleFor(x => x.UpdatedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Updated date cannot be in the future");
    }
}