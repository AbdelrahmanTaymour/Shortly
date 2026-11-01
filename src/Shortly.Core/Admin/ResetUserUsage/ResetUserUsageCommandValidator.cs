using FluentValidation;

namespace Shortly.Core.Admin.ResetUserUsage;

internal sealed class ResetUserUsageCommandValidator : AbstractValidator<ResetUserUsageCommand>
{
    public ResetUserUsageCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}