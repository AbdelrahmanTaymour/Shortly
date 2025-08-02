namespace Shortly.Domain.Entities;

public class OrganizationAuditLog
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ActorId { get; set; }
    public string? Event { get; set; }
    public string? TargetEntity { get; set; }
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Organization? Organization { get; set; }
    public OrganizationMember? Actor { get; set; }
}