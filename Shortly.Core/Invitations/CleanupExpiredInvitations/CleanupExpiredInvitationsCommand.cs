using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Invitations.CleanupExpiredInvitations;

public sealed record CleanupExpiredInvitationsCommand : ICommand<int>;
