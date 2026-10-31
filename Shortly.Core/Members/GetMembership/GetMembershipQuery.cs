using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Members.GetOrganizationMembers;

namespace Shortly.Core.Members.GetMembership;

public sealed record GetMembershipQuery(Guid OrganizationId, Guid UserId) : IQuery<OrganizationMemberResponse>;
