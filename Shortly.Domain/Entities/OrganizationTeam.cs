namespace Shortly.Domain.Entities;

public class OrganizationTeam
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TeamManagerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization? Organization { get; set; }
    public OrganizationMember? TeamManager { get; set; }
    public ICollection<OrganizationTeamMember>? TeamMembers { get; set; }
}