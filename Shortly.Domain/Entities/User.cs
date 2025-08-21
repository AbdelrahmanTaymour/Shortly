using System.ComponentModel.DataAnnotations;
using Shortly.Domain.Enums;

namespace Shortly.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public enSubscriptionPlan SubscriptionPlanId { get; set; } = enSubscriptionPlan.Free;
    public long Permissions { get; set; } = (long)enPermissions.BasicUser;
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; } = false;
    public DateTime? LastLoginAt { get; set; } = null;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; } = null;
    public Guid? DeletedBy { get; set; } = null;

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