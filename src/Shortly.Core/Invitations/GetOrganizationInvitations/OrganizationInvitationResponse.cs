using Shortly.Domain.Enums;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.GetOrganizationInvitations;

public sealed record OrganizationInvitationResponse(
    Guid Id, Guid OrganizationId, string InvitedUserEmail, MemberRole InvitedUserRole,
    string InvitedByUsername, enInvitationStatus Status, DateTime? RegisteredAt, DateTime ExpiresAt, DateTime CreatedAt);