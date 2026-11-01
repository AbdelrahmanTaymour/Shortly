using FluentValidation;

namespace Shortly.Core.Organizations.DeleteOrganization;

internal sealed class DeleteOrganizationCommandValidator
    : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();
    }
}
