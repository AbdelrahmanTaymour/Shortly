using Shortly.Domain.Entities;
using Shortly.Domain.Enums;
using Shortly.Domain.Members;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Domain.Users;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public enSubscriptionPlan SubscriptionPlanId { get; set; } = enSubscriptionPlan.Free;
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // OAuth fields
    public string? GoogleId { get; set; }
    public string? GoogleProfilePicture { get; set; }
    public bool IsOAuthUser { get; set; } = false;
    
    // Navigation properties
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    public UserProfile? Profile { get; set; }
    public UserSecurity? UserSecurity { get; set; }
    public UserUsage? UserUsage { get; set; }
    public ICollection<ShortUrl>? OwnedShortUrls { get; set; }
    public ICollection<UserAuditLog>? AuditLogs { get; set; }
    public ICollection<RefreshToken>? RefreshTokens { get; set; }
    public ICollection<OrganizationMember>? OrganizationMemberships { get; set; }
}


//TODO: public string? PasswordSalt { get; set; }