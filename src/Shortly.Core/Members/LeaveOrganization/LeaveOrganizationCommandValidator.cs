using FluentValidation;

namespace Shortly.Core.Members.LeaveOrganization;

internal sealed class LeaveOrganizationCommandValidator : AbstractValidator<LeaveOrganizationCommand>
{
    public LeaveOrganizationCommandValidator() => RuleFor(x => x.OrganizationId).NotEmpty();
}
