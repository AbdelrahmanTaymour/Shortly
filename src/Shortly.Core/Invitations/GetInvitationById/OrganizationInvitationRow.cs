using Shortly.Core.Invitations.GetOrganizationInvitations;
using Shortly.Domain.Invitations;
using Shortly.Domain.Members;

namespace Shortly.Core.Invitations.GetInvitationById;

internal sealed class OrganizationInvitationRow
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string InvitedUserEmail { get; init; } = null!;
    public int InvitedUserRole { get; init; }
    public string InvitedByUsername { get; init; } = null!;
    public int Status { get; init; }
    public DateTime? RegisteredAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }

    public OrganizationInvitationResponse ToResponse() => new(
        Id, OrganizationId, InvitedUserEmail, (MemberRole)InvitedUserRole, InvitedByUsername,
        (enInvitationStatus)Status, RegisteredAt, ExpiresAt, CreatedAt);
}