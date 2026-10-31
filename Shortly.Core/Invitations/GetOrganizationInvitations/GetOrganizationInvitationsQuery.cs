using Shortly.Core.Abstractions.Messaging;
using Shortly.Domain.Abstractions;

namespace Shortly.Core.Invitations.GetOrganizationInvitations;

public sealed record GetOrganizationInvitationsQuery(Guid OrganizationId, int Page = 1, int PageSize = 20) : IQuery<PagedResponse<OrganizationInvitationResponse>>;
