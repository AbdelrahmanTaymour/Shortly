using Shortly.Core.Abstractions.Clock;
using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Invitations;

namespace Shortly.Core.Invitations.CleanupExpiredInvitations;

internal sealed class CleanupExpiredInvitationsCommandHandler(
    IOrganizationInvitationRepository organizationInvitationRepository,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CleanupExpiredInvitationsCommand, int>
{
    public async Task<Result<int>> Handle(CleanupExpiredInvitationsCommand request, CancellationToken cancellationToken)
    {
        return await organizationInvitationRepository.CleanupExpiredInvitations(dateTimeProvider.UtcNow,
            cancellationToken);
    }
}