using FluentValidation;

namespace Shortly.Core.Account.InitiateEmailChange;

internal sealed class InitiateEmailChangeCommandValidator : AbstractValidator<InitiateEmailChangeCommand>
{
    public InitiateEmailChangeCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.CurrentPassword)
            .NotEmpty();
    }
}