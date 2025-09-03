using FluentValidation;
using Shortly.Core.DTOs.AuthDTOs.Email;

namespace Shortly.Core.Validators.Auth.Email;

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