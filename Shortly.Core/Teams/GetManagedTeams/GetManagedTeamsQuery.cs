using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Teams.GetTeam;

namespace Shortly.Core.Teams.GetManagedTeams;

public sealed record GetMyManagedTeamsQuery : IQuery<IReadOnlyList<OrganizationTeamResponse>>;
