using FluentValidation;

namespace Shortly.Core.Admin.BulkDeactivateUsers;

internal sealed class BulkDeactivateUsersCommandValidator : AbstractValidator<BulkDeactivateUsersCommand>
{
    public BulkDeactivateUsersCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty();

        RuleForEach(x => x.UserIds)
            .NotEmpty();
    }
}