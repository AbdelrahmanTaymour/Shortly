using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Invitations.ResendInvitation;

public sealed record ResendInvitationCommand(Guid OrganizationId, Guid InvitationId) : ICommand;
