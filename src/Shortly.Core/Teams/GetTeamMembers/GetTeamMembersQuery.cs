using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Teams.GetTeamMembers;

public sealed record GetTeamMembersQuery(Guid TeamId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResponse<OrganizationTeamMemberResponse>>;
