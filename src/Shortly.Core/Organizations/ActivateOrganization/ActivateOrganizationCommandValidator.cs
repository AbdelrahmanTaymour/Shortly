using FluentValidation;

namespace Shortly.Core.Organizations.ActivateOrganization;

internal sealed class ActivateOrganizationCommandValidator : AbstractValidator<ActivateOrganizationCommand>
{
    public ActivateOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
