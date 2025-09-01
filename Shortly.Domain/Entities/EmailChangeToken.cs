namespace Shortly.Domain.Entities;

public class EmailChangeToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Token { get; set; }
    public required string OldEmail { get; set; }
    public required string NewEmail { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
    
    // Navigation property
    public User User { get; set; }
}