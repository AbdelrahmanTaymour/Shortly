using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;
using Shortly.Core.Teams.GetTeam;

namespace Shortly.Core.Teams.GetOrganizationTeams;

public sealed record GetOrganizationTeamsQuery(Guid OrganizationId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResponse<OrganizationTeamResponse>>;
