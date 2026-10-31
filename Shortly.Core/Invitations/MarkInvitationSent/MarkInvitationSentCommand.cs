using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Invitations.MarkInvitationSent;

public sealed record MarkInvitationSentCommand(Guid InvitationId) : ICommand;
