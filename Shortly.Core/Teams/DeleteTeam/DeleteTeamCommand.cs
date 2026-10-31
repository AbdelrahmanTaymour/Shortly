using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.DeleteTeam;

public sealed record DeleteTeamCommand(Guid TeamId) : ICommand;
