using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.UpdateTeam;

public sealed record UpdateTeamCommand(Guid OrganizationId, Guid TeamId, string? Name, string? Description) : ICommand;
