using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public enUserRole Role { get; set; }
    public enPermissions CustomPermissions { get; set; } = enPermissions.None;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Organization Organization { get; set; }
    public User User { get; set; }
}