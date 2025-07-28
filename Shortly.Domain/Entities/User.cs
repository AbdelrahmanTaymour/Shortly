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
    public enSubscriptionPlan SubscriptionPlan { get; set; } = enSubscriptionPlan.Free;
    public enUserRole Role { get; set; } = enUserRole.StandardUser;
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? TimeZone { get; set; } = "UTC";
    public string? ProfilePictureUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; init; }
    public Guid? DeletedBy { get; set; }
    
    // Usage tracking for subscription limits
    public int MonthlyLinksCreated { get; set; } = 0;
    public int TotalLinksCreated { get; set; } = 0;
    public DateTime MonthlyResetDate { get; set; } = DateTime.UtcNow.AddMonths(1);
    
    // Security
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    
    // Navigation properties
    public ICollection<ShortUrl> ShortUrls { get; set; } = new List<ShortUrl>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Organization> OwnedOrganizations { get; set; } = new List<Organization>();
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = new List<OrganizationMember>();
    
}


//TODO: public string? PasswordSalt { get; set; }