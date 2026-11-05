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
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        return new RefreshToken(Guid.NewGuid(), userId, tokenHash, utcNow.Add(lifetime), organizationId, utcNow);
    }

    /// <summary>Revokes the token. Revoking an already revoked token changes nothing, so the first revocation time is kept.</summary>
    public void Revoke(DateTime utcNow)
    {
        if (IsRevoked) return;

        IsRevoked = true;
        RevokedAt = utcNow;
        UpdatedAt = utcNow;
    }

    /// <summary>Replaces the token hash and extends the expiry. A revoked or expired token cannot be rotated.</summary>
    public Result Rotate(string newTokenHash, TimeSpan lifetime, DateTime utcNow)
    {
        if (!IsActive(utcNow)) return Result.Failure(UserErrors.InvalidRefreshToken);

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lifetime, TimeSpan.Zero);

        TokenHash = newTokenHash;
        ExpiresAt = utcNow.Add(lifetime);
        UpdatedAt = utcNow;
        return Result.Success();
    }
}