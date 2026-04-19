namespace Shortly.Core.Security.DTOs;

/// <summary>
/// Response model for unlocking a user account.
/// </summary>
public record UnlockUserResponse : SecurityActionResponse
{
    public DateTime UnlockedAt { get; set; }
}