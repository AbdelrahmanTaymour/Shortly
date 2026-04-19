using FluentValidation;
using Shortly.Core.Auth.DTOs.Email;

namespace Shortly.Core.Auth.Validators.Email;

public class SendEmailVerificationRequestValidator : AbstractValidator<SendEmailVerificationRequest>
{
    public SendEmailVerificationRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Please provide a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}