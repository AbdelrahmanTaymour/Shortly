using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;

namespace Shortly.Core.Invitations.ValidateInvitationToken;

internal sealed class ValidateInvitationTokenQueryHandler(
    IOrganizationInvitationRepository invitationRepository, ITokenHasher tokenHasher, IDateTimeProvider dateTimeProvider)
    : IQueryHandler<ValidateInvitationTokenQuery, bool>
{
    public async Task<Result<bool>> Handle(ValidateInvitationTokenQuery request, CancellationToken cancellationToken)
    {
        var invitation = await invitationRepository.GetByTokenHashAsync(tokenHasher.Hash(request.Token), cancellationToken);
        return invitation is not null && invitation.IsActionable(dateTimeProvider.UtcNow);
    }
}