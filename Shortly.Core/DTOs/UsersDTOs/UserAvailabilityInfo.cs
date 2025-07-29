namespace Shortly.Core.DTOs.UsersDTOs;

public record UserAvailabilityInfo(bool Exists, bool IsActive, bool IsLocked)
{
    public bool IsAvailable => Exists && IsActive && !IsLocked;
};