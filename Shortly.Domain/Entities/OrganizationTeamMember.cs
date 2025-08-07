namespace Shortly.Domain.Entities;

public class OrganizationTeamMember
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid MemberId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public OrganizationTeam? Team { get; set; }
    public OrganizationMember? Member { get; set; }
}