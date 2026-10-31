using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.GetOrganizationTeams;
using Shortly.Core.Teams.GetTeam;

namespace Shortly.Core.Teams.GetManagedTeams;

public sealed record GetManagedTeamsQuery(Guid ManagerId) : IQuery<IReadOnlyList<OrganizationTeamResponse>>;
