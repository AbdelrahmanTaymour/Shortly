using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class UserActionToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public enUserActionTokenType TokenType { get; set; } // EmailVerification, PasswordReset, etc.
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}