using FluentValidation;

namespace Shortly.Core.Account.ConfirmEmailChange;

internal sealed class ConfirmEmailChangeCommandValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}