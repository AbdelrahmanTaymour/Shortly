using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.GetTeam;

public sealed record GetTeamQuery(Guid OrganizationId, Guid TeamId) : IQuery<OrganizationTeamResponse>;
