using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UserDTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public enSubscriptionPlan SubscriptionPlan { get; set; }
    public enUserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? TimeZone { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public int MonthlyLinksCreated { get; set; }
    public int TotalLinksCreated { get; set; }
    public DateTime MonthlyResetDate { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}