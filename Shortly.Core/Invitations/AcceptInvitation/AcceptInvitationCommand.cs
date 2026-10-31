using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Invitations.AcceptInvitation;

public sealed record AcceptInvitationCommand(string Token) : ICommand;
