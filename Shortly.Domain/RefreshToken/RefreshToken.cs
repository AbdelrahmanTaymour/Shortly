using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Domain.RefreshToken;

public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAt, Guid? organizationId, DateTime utcNow)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        OrganizationId = organizationId;
        CreatedAt = utcNow;
    }

    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? OrganizationId { get; private set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Issue(Guid userId, string tokenHash, TimeSpan lifetime, Guid? organizationId, DateTime utcNow)
        => new(Guid.NewGuid(), userId, tokenHash, utcNow.Add(lifetime), organizationId, utcNow);

    public void Revoke(DateTime utcNow)
    {
        IsRevoked = true;
        RevokedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Rotate(string newTokenHash, TimeSpan lifetime, DateTime utcNow)
    {
        TokenHash = newTokenHash;
        ExpiresAt = utcNow.Add(lifetime);
        UpdatedAt = utcNow;
    }
}