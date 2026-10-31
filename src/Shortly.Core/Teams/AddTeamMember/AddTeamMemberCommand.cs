using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.AddTeamMember;

public sealed record AddTeamMemberCommand(Guid OrganizationId, Guid TeamId, Guid MemberId) : ICommand;
