using Shortly.Core.Abstractions.Messaging;

namespace Shortly.Core.Teams.GetTeamMembers;

public sealed record GetTeamMembersQuery(Guid TeamId) : IQuery<IReadOnlyList<OrganizationTeamMemberResponse>>;
