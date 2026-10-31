using Shortly.Core.Abstractions.Messaging;
using Shortly.Core.Invitations.GetOrganizationInvitations;

namespace Shortly.Core.Invitations.GetInvitationById;

public sealed record GetInvitationByIdQuery(Guid InvitationId) : IQuery<OrganizationInvitationResponse>;
