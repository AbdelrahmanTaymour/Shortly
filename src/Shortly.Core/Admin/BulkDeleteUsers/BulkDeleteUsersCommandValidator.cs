using FluentValidation;

namespace Shortly.Core.Admin.BulkDeleteUsers;

internal sealed class BulkDeleteUsersCommandValidator : AbstractValidator<BulkDeleteUsersCommand>
{
    public BulkDeleteUsersCommandValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty();

        RuleForEach(x => x.UserIds)
            .NotEmpty();
    }
}