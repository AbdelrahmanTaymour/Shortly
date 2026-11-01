using FluentValidation;

namespace Shortly.Core.Organizations.DeactivateOrganization;

internal sealed class DeactivateOrganizationCommandValidator
    : AbstractValidator<DeactivateOrganizationCommand>
{
    public DeactivateOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();
    }
}