namespace Shortly.Core.DTOs.UsersDTOs.Security;

public record LockUserResponse : SecurityActionResponse
{
    /// <summary>
    /// The UTC date and time until which the account is locked.
    /// </summary>
    public DateTime LockedUntil { get; set; }

    /// <summary>
    /// Indicates whether existing tokens were revoked as part of the lock operation.
    /// </summary>
    public bool TokensRevoked { get; set; } = true;
}