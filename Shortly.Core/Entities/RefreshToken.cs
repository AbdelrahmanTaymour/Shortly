namespace Shortly.Core.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } // Store full refresh token string
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid UserId { get; set; } // Foreign key to your user table
        
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}