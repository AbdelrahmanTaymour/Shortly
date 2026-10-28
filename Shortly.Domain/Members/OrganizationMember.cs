
using Shortly.Domain.Abstractions;
using Shortly.Domain.Entities;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Domain.Members;

public class OrganizationMember : Entity<Guid>
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid InvitedBy { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public Organization? Organization { get; set; }
    public User User { get; set; }
    public ICollection<ShortUrl> CreatedShortUrls { get; set; }
    public ICollection<OrganizationTeamMember> JoinedTeams { get; set; }
}