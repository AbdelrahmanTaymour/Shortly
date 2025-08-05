namespace Shortly.Domain.Entities;

public class UserSecurity
{
    public Guid UserId { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User? User { get; set; }
}

    //TODO: public string[]? TwoFactorRecoveryCodes { get; set; }
