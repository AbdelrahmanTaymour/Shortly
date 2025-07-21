namespace Shortly.Core.Entities;

/// <summary>
/// Enhanced User entity with comprehensive role-based access control and security features
/// </summary>
public class User
{
    public Guid Id { get; set; }
    
    // Basic Information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Renamed from Password for clarity
    
    // Profile Information
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public string? TimeZone { get; set; }
    public string? PreferredLanguage { get; set; } = "en";
    
    // Contact Information
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    
    // Security & Authentication
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string[]? TwoFactorRecoveryCodes { get; set; }
    public DateTime? LastPasswordChange { get; set; }
    public bool RequirePasswordChange { get; set; }
    
    // Account Status
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete
    public Guid? DeletedBy { get; set; }
    public string? DeletionReason { get; set; }
    
    // Login Security
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; } = true;
    public string? LastLoginIp { get; set; }
    public string? LastLoginUserAgent { get; set; }
    
    // API Access
    public string? ApiKey { get; set; }
    public DateTime? ApiKeyCreatedAt { get; set; }
    public DateTime? ApiKeyLastUsed { get; set; }
    public bool ApiAccessEnabled { get; set; }
    
    // Subscription & Billing
    public Guid? SubscriptionId { get; set; }
    public SubscriptionTier CurrentTier { get; set; } = SubscriptionTier.Free;
    public string? StripeCustomerId { get; set; }
    
    // Preferences
    public bool EmailNotifications { get; set; } = true;
    public bool MarketingEmails { get; set; } = false;
    public bool WeeklyReports { get; set; } = true;
    public string? NotificationPreferences { get; set; } // JSON string
    
    // Usage Statistics
    public int TotalUrlsCreated { get; set; }
    public int TotalClicks { get; set; }
    public DateTime? LastUrlCreated { get; set; }
    
    // GDPR & Privacy
    public bool TermsAccepted { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }
    public bool PrivacyPolicyAccepted { get; set; }
    public DateTime? PrivacyPolicyAcceptedAt { get; set; }
    public bool DataProcessingConsent { get; set; }
    public DateTime? DataProcessingConsentAt { get; set; }
    
    // Navigation Properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<ShortUrl> ShortUrls { get; set; } = new List<ShortUrl>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public ICollection<UserActivity> UserActivities { get; set; } = new List<UserActivity>();
    public ICollection<UserLoginHistory> LoginHistory { get; set; } = new List<UserLoginHistory>();
    public ICollection<CustomDomain> CustomDomains { get; set; } = new List<CustomDomain>();
    public ICollection<Webhook> Webhooks { get; set; } = new List<Webhook>();
    public Subscription? Subscription { get; set; }
    
    // Computed Properties
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayNameOrUsername => !string.IsNullOrEmpty(DisplayName) ? DisplayName : Username;
    public string DisplayNameOrFullName => !string.IsNullOrEmpty(DisplayName) ? DisplayName : FullName;
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
    public bool IsDeleted => DeletedAt.HasValue;
    public bool IsEmailVerified => EmailConfirmed;
    public bool Is2FAEnabled => TwoFactorEnabled && !string.IsNullOrEmpty(TwoFactorSecret);
    
    // Helper Methods
    public bool HasRole(string roleName)
    {
        return UserRoles.Any(ur => ur.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool HasPermission(string permissionName)
    {
        return UserRoles.Any(ur => ur.Role.RolePermissions.Any(rp => 
            rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase)));
    }
    
    public IEnumerable<string> GetRoles()
    {
        return UserRoles.Select(ur => ur.Role.Name);
    }
    
    public IEnumerable<string> GetPermissions()
    {
        return UserRoles.SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name)).Distinct();
    }
}

/// <summary>
/// User status enumeration
/// </summary>
public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    PendingVerification = 4,
    Banned = 5,
    Deleted = 6
}