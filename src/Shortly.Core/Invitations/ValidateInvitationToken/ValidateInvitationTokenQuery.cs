using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Invitations.ValidateInvitationToken;

public sealed record ValidateInvitationTokenQuery(string Token) : IQuery<bool>;
