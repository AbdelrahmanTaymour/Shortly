using FluentValidation;
using Shortly.Core.DTOs.UserDTOs;

namespace Shortly.Core.Validators;

public class TwoFactorSetupRequestValidator : AbstractValidator<TwoFactorSetupRequest>
{
    public TwoFactorSetupRequestValidator()
    {
        RuleFor(x => x.VerificationCode)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits.");
    }
}

public class VerifyTwoFactorRequestValidator : AbstractValidator<VerifyTwoFactorRequest>
{
    public VerifyTwoFactorRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Two-factor code is required.")
            .Length(6).WithMessage("Two-factor code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Two-factor code must contain only digits.");
    }
}

public class DisableTwoFactorRequestValidator : AbstractValidator<DisableTwoFactorRequest>
{
    public DisableTwoFactorRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");

        RuleFor(x => x.TwoFactorCode)
            .NotEmpty().WithMessage("Two-factor code is required.")
            .Length(6).WithMessage("Two-factor code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Two-factor code must contain only digits.");
    }
}