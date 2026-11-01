using FluentValidation;

namespace Shortly.Core.Admin.BulkActivateUsers;

internal sealed class BulkActivateUsersCommandValidator : AbstractValidator<BulkActivateUsersCommand>
{
    public BulkActivateUsersCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty();

        RuleForEach(x => x.UserIds)
            .NotEmpty();
    }
}