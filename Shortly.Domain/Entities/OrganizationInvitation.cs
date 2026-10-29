using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Domain.Entities;

public class OrganizationInvitation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string InvitedUserEmail { get; set; }
    public MemberRole InvitedUserRoleId { get; set; } = MemberRole.Member;
    public enPermissions InvitedUserPermissions { get; set; } = enPermissions.None;
    public Guid InvitedBy { get; set; }
    public enInvitationStatus Status { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed Property (calculated dynamically in memory)
    public bool IsExpired => 
        (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) || 
        Status == enInvitationStatus.Failure || 
        Status == enInvitationStatus.Registered || 
        Status == enInvitationStatus.Rejected;

    // Navigation properties
    public Organization? Organization { get; set; }
    public OrganizationMember? InvitedByMember { get; set; }
    public MemberRole? InvitedUserRole { get; set; }
}