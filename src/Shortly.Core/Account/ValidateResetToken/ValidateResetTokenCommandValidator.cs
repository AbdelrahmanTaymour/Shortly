using FluentValidation;

namespace Shortly.Core.Account.ValidateResetToken;

internal sealed class ValidateResetTokenCommandValidator : AbstractValidator<ValidateResetTokenCommand>
{
    public ValidateResetTokenCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}