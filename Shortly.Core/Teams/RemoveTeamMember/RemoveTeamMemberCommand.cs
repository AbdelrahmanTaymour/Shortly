using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.RemoveTeamMember;

public sealed record RemoveTeamMemberCommand(Guid TeamId, Guid MemberId) : ICommand;
