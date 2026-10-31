using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.GetTeam;

namespace Shortly.Core.Teams.GetOrganizationTeams;

public sealed record GetOrganizationTeamsQuery(Guid OrganizationId) : IQuery<IReadOnlyList<OrganizationTeamResponse>>;
