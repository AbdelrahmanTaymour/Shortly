using FluentValidation;

namespace Shortly.Core.Organizations.SwitchOrganization;

internal sealed class SwitchOrganizationCommandValidator
    : AbstractValidator<SwitchOrganizationCommand>
{
    public SwitchOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();
    }
}
