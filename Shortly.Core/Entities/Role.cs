namespace Shortly.Core.Entities;

/// <summary>
/// Role entity for role-based access control system
/// </summary>
public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
    
    // Role Properties
    public int Priority { get; set; } // Higher number = higher priority
    public bool IsSystemRole { get; set; } // Cannot be deleted
    public bool IsActive { get; set; } = true;
    public string? Color { get; set; } // For UI display
    public string? Icon { get; set; } // For UI display
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    
    // Navigation Properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    
    // Helper Methods
    public bool HasPermission(string permissionName)
    {
        return RolePermissions.Any(rp => rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }
    
    public IEnumerable<string> GetPermissions()
    {
        return RolePermissions.Select(rp => rp.Permission.Name);
    }
}

/// <summary>
/// Permission entity for granular access control
/// </summary>
public class Permission
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DisplayName { get; set; }
    
    // Permission Organization
    public string Category { get; set; } = string.Empty; // e.g., "URLs", "Analytics", "Admin"
    public string? SubCategory { get; set; }
    public int SortOrder { get; set; }
    
    // Permission Properties
    public bool IsSystemPermission { get; set; } // Cannot be deleted
    public bool IsActive { get; set; } = true;
    public PermissionType Type { get; set; } = PermissionType.Feature;
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation Properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Junction table for Role-Permission many-to-many relationship
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
    
    // Navigation Properties
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

/// <summary>
/// Junction table for User-Role many-to-many relationship with additional metadata
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    
    // Assignment Metadata
    public DateTime AssignedAt { get; set; }
    public Guid? AssignedBy { get; set; }
    public DateTime? ExpiresAt { get; set; } // For temporary role assignments
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    
    // Navigation Properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    
    // Helper Properties
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
    public bool IsCurrentlyActive => IsActive && !IsExpired;
}

/// <summary>
/// User session tracking for security and analytics
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    
    // Session Information
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Device & Location Information
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceType { get; set; }
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    
    // Security
    public bool IsTrusted { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }
    
    // Navigation Properties
    public User User { get; set; } = null!;
}

/// <summary>
/// User activity logging for audit trails
/// </summary>
public class UserActivity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; } // JSON string
    public string? OldValues { get; set; } // JSON string for changes
    public string? NewValues { get; set; } // JSON string for changes
    
    // Context Information
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    
    // Risk Assessment
    public ActivityRiskLevel RiskLevel { get; set; } = ActivityRiskLevel.Low;
    public bool RequiresReview { get; set; }
    public bool IsReviewed { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    
    // Navigation Properties
    public User User { get; set; } = null!;
}

/// <summary>
/// User login history for security monitoring
/// </summary>
public class UserLoginHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    // Login Information
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
    public LoginResult Result { get; set; }
    public string? FailureReason { get; set; }
    
    // Device & Location Information
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? DeviceFingerprint { get; set; }
    
    // Security Assessment
    public bool IsNewLocation { get; set; }
    public bool IsNewDevice { get; set; }
    public bool IsSuspicious { get; set; }
    public RiskLevel RiskScore { get; set; } = RiskLevel.Low;
    
    // Two-Factor Authentication
    public bool TwoFactorUsed { get; set; }
    public string? TwoFactorMethod { get; set; }
    
    // Navigation Properties
    public User User { get; set; } = null!;
}

/// <summary>
/// Permission type enumeration
/// </summary>
public enum PermissionType
{
    Feature = 1,    // Access to specific features
    Data = 2,       // Access to specific data
    Action = 3,     // Ability to perform specific actions
    Admin = 4       // Administrative permissions
}

/// <summary>
/// Activity risk level enumeration
/// </summary>
public enum ActivityRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Login result enumeration
/// </summary>
public enum LoginResult
{
    Success = 1,
    FailedPassword = 2,
    FailedUsername = 3,
    AccountLocked = 4,
    AccountDisabled = 5,
    RequiresTwoFactor = 6,
    Failed2FA = 7,
    Expired = 8
}

/// <summary>
/// Risk level enumeration
/// </summary>
public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}