using FluentValidation;

namespace Shortly.Core.Admin.ResetOrganizationUsage;

internal sealed class ResetOrganizationUsageCommandValidator : AbstractValidator<ResetOrganizationUsageCommand>
{
    public ResetOrganizationUsageCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();
    }
}