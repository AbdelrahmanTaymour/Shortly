using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.ChangeTeamManager;

public sealed record ChangeTeamManagerCommand(Guid OrganizationId, Guid TeamId, Guid NewManagerId) : ICommand;
