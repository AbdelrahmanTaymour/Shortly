using Shortly.Domain.Users;

namespace Shortly.Domain.Tokens;

public class UserActionToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public enUserActionTokenType TokenType { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }

    // Only populated when TokenType == EmailChange
    public string? NewEmail { get; set; }

    public User User { get; set; } = null!;
}