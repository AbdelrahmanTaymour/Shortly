namespace Shortly.Domain.Entities;

public class UserAuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Action { get; set; }
    public string? Details { get; set; }
    public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Navigation properties
    public User? User { get; set; }
}