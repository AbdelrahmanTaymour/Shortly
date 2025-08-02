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
    public enSubscriptionPlan SubscriptionPlanId { get; set; }
    public long Permissions { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; init; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; init; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation properties
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    public UserProfile? Profile { get; set; }
    public UserSecurity? UserSecurity { get; set; }
    public UserUsage? UserUsage { get; set; }
    public ICollection<ShortUrl> OwnedShortUrls { get; set; }
    public ICollection<UserAuditLog> AuditLogs { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; }
}


//TODO: public string? PasswordSalt { get; set; }