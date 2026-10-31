using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.DeleteTeam;

public sealed record DeleteTeamCommand(Guid OrganizationId, Guid TeamId) : ICommand;
