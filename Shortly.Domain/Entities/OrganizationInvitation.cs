using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class OrganizationInvitation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string InvitedUserEmail { get; set; }
    public enUserRole InvitedUserRoleId { get; set; } = enUserRole.Member;
    public enPermissions InvitedUserPermissions { get; set; } = enPermissions.None;
    public Guid InvitedBy { get; set; }
    public enInvitationStatus Status { get; set; } = enInvitationStatus.Pending;
    public DateTime? RegisteredAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed Property:
    public bool IsExpired { get; set; } //HasComputedColumnSql("CASE WHEN [ExpiresAt] < GETUTCDATE() THEN 1 ELSE 0 END");

    // Navigation properties
    public Organization? Organization { get; set; }
    public OrganizationMember? InvitedByMember { get; set; }
    public Role? InvitedUserRole { get; set; }
}