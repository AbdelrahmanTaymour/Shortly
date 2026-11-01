using FluentValidation;

namespace Shortly.Core.Admin.HardDeleteUser;

internal sealed class HardDeleteUserCommandValidator : AbstractValidator<HardDeleteUserCommand>
{
    public HardDeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}