using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.GetTeam;

public sealed record GetTeamQuery(Guid TeamId) : IQuery<OrganizationTeamResponse>;
