using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public enUserRole RoleId { get; set; } = enUserRole.Member;
    public enPermissions CustomPermissions { get; set; } = enPermissions.None;
    public bool IsActive { get; set; } = true;
    public Guid InvitedBy { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization? Organization { get; set; }
    public User User { get; set; }
    public Role Role { get; set; }
    public ICollection<ShortUrl> CreatedShortUrls { get; set; }
    public ICollection<OrganizationTeamMember> JoinedTeams { get; set; }
}