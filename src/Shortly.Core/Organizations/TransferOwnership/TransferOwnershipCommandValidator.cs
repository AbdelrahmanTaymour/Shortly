using FluentValidation;

namespace Shortly.Core.Organizations.TransferOwnership;

internal sealed class TransferOwnershipCommandValidator
    : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty();

        RuleFor(x => x.NewOwnerId)
            .NotEmpty();
    }
}
