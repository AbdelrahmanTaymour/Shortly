using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Invitations.CancelInvitation;

public sealed record CancelInvitationCommand(Guid OrganizationId, Guid InvitationId) : ICommand;
