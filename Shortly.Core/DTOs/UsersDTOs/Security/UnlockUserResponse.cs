namespace Shortly.Core.DTOs.UsersDTOs.Security;

/// <summary>
/// Response model for unlocking a user account.
/// </summary>
public record UnlockUserResponse : SecurityActionResponse
{
    /// <summary>
    /// The UTC date and time when the account was unlocked.
    /// </summary>
    public DateTime UnlockedAt { get; set; }
}