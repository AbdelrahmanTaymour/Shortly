using FluentValidation;

namespace Shortly.Core.Admin.UnlockUser;

internal sealed class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}