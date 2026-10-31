using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Invitations.RejectInvitation;

public sealed record RejectInvitationCommand(string Token) : ICommand;
