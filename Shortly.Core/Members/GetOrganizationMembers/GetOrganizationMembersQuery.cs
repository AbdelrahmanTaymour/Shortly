using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Members.GetOrganizationMembers;

public sealed record GetOrganizationMembersQuery(Guid OrganizationId, int Page = 1, int PageSize = 20) : IQuery<PagedResponse<OrganizationMemberResponse>>;
