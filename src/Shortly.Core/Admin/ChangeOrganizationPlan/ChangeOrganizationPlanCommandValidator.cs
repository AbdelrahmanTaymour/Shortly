using FluentValidation;

namespace Shortly.Core.Admin.ChangeOrganizationPlan;

public class ChangeOrganizationPlanCommandValidator : AbstractValidator<ChangeOrganizationPlanCommand>
{
    public ChangeOrganizationPlanCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();

        RuleFor(x => x.SubscriptionPlanId)
            .IsInEnum()
            .WithMessage("The specified subscription plan is invalid.");
    }
}