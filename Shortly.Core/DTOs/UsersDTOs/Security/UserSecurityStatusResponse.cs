namespace Shortly.Core.DTOs.UsersDTOs.Security;

public record UserSecurityStatusResponse
{
    /// <summary>
    /// The user ID being queried.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Indicates whether the user account is currently locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// The UTC date and time until which the account is locked (if applicable).
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// The reason why the account was locked (if applicable).
    /// </summary>
    public string? LockReason { get; set; }

    /// <summary>
    /// The current number of failed login attempts.
    /// </summary>
    public int FailedAttemptsCount { get; set; }
    
    /// <summary>
    /// Count of days until unlocking
    /// </summary>
    public int DaysUntilUnlock { get; set; }
}