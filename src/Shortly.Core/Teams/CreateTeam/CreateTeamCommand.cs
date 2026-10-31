using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.CreateTeam;

public sealed record CreateTeamCommand(Guid OrganizationId, Guid TeamManagerId, string Name, string? Description) : ICommand<Guid>;
