using FluentValidation;

namespace Shortly.Core.Admin.LockUser;

internal sealed class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.Duration)
            .GreaterThan(TimeSpan.Zero);
    }
}