using System.ComponentModel.DataAnnotations;
using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name {get; set;}
    public string Email { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public enSubscriptionPlan SubscriptionPlan { get; set; }
    public enUserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? TimeZone { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; init; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; init; }
    public Guid? DeletedBy { get; set; }
    
    // Usage tracking for subscription limits
    public int MonthlyLinksCreated { get; set; }
    public int TotalLinksCreated { get; set; }
    public DateTime MonthlyResetDate { get; set; }
    
    // Security
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    
    // Navigation properties
    public ICollection<ShortUrl> ShortUrls { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
    public ICollection<Organization> OwnedOrganizations { get; set; }
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; }
    
}


//TODO: public string? PasswordSalt { get; set; }