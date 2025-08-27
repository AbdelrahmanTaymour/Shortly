using System.ComponentModel.DataAnnotations;
using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public int MemberLimit { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public bool IsSubscribed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid OwnerId { get; set; }
    
    public bool IsDeleted => DeletedAt != null;

    // Navigation properties
    public User? Owner { get; set; }
    public ICollection<OrganizationMember> Members { get; set; }
    public ICollection<OrganizationTeam> Teams { get; set; }
    public ICollection<ShortUrl> ShortUrls { get; set; }
    public ICollection<OrganizationAuditLog> AuditLogs { get; set; }
}