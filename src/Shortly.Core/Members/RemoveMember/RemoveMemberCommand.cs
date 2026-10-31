using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Members.RemoveMember;

public sealed record RemoveMemberCommand(Guid OrganizationId, Guid UserId) : ICommand;
