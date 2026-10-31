using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.AddTeamMember;

public sealed record AddTeamMemberCommand(Guid TeamId, Guid MemberId) : ICommand;
