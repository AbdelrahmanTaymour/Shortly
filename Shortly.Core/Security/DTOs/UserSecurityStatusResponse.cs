namespace Shortly.Core.Security.DTOs;

public record UserSecurityStatusResponse
{
    public Guid UserId { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LockReason { get; set; }
    public int FailedAttemptsCount { get; set; }
    public int DaysUntilUnlock { get; set; }
}