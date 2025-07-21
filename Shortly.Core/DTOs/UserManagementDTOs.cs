using Shortly.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace Shortly.Core.DTOs;

// User Management DTOs
public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Username,
    string? DisplayName,
    string? Bio,
    string? ProfileImageUrl,
    string? Company,
    string? JobTitle,
    string? Website,
    string? Location,
    string? TimeZone,
    string? PreferredLanguage,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    UserStatus Status,
    SubscriptionTier CurrentTier,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastLoginAt,
    DateTime? LastActivityAt,
    int TotalUrlsCreated,
    int TotalClicks,
    bool TermsAccepted,
    bool PrivacyPolicyAccepted,
    List<string> Roles,
    List<string> Permissions
)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayNameOrUsername => !string.IsNullOrEmpty(DisplayName) ? DisplayName : Username;
    public bool IsLocked => Status == UserStatus.Suspended || Status == UserStatus.Banned;
    public bool IsActive => Status == UserStatus.Active;
}

public record CreateUserRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    [Required] string Username,
    [Required, MinLength(8)] string Password,
    string? DisplayName = null,
    string? PhoneNumber = null,
    string? Company = null,
    string? JobTitle = null,
    List<string>? Roles = null,
    bool SendWelcomeEmail = true,
    bool RequireEmailVerification = true
);

public record UpdateUserRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? Username = null,
    string? DisplayName = null,
    string? Bio = null,
    string? ProfileImageUrl = null,
    string? Company = null,
    string? JobTitle = null,
    string? Website = null,
    string? Location = null,
    string? TimeZone = null,
    string? PreferredLanguage = null,
    string? PhoneNumber = null,
    bool? EmailNotifications = null,
    bool? MarketingEmails = null,
    bool? WeeklyReports = null
);

public record UserSearchRequest(
    string? SearchTerm = null,
    UserStatus? Status = null,
    SubscriptionTier? Tier = null,
    List<string>? Roles = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null,
    DateTime? LastLoginAfter = null,
    DateTime? LastLoginBefore = null,
    string? SortBy = "CreatedAt",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword,
    [Required] string ConfirmPassword
);

// Role Management DTOs
public record RoleResponse(
    Guid Id,
    string Name,
    string? DisplayName,
    string? Description,
    int Priority,
    bool IsSystemRole,
    bool IsActive,
    string? Color,
    string? Icon,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Permissions,
    int UserCount
);

public record CreateRoleRequest(
    [Required] string Name,
    string? DisplayName = null,
    string? Description = null,
    int Priority = 500,
    string? Color = null,
    string? Icon = null,
    List<string>? Permissions = null
);

public record UpdateRoleRequest(
    string? Name = null,
    string? DisplayName = null,
    string? Description = null,
    int? Priority = null,
    bool? IsActive = null,
    string? Color = null,
    string? Icon = null
);

public record RoleSearchRequest(
    string? SearchTerm = null,
    bool? IsSystemRole = null,
    bool? IsActive = null,
    string? SortBy = "Priority",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20
);

// Permission Management DTOs
public record PermissionResponse(
    Guid Id,
    string Name,
    string? DisplayName,
    string? Description,
    string Category,
    string? SubCategory,
    PermissionType Type,
    bool IsSystemPermission,
    bool IsActive,
    int SortOrder
);

public record CreatePermissionRequest(
    [Required] string Name,
    string? DisplayName = null,
    string? Description = null,
    [Required] string Category = "General",
    string? SubCategory = null,
    PermissionType Type = PermissionType.Feature,
    int SortOrder = 100
);

public record UpdatePermissionRequest(
    string? Name = null,
    string? DisplayName = null,
    string? Description = null,
    string? Category = null,
    string? SubCategory = null,
    PermissionType? Type = null,
    bool? IsActive = null,
    int? SortOrder = null
);

// Session Management DTOs
public record UserSessionResponse(
    Guid Id,
    DateTime CreatedAt,
    DateTime LastActivity,
    DateTime? ExpiresAt,
    bool IsActive,
    string? IpAddress,
    string? DeviceType,
    string? Browser,
    string? OperatingSystem,
    string? Country,
    string? City,
    bool IsTrusted,
    bool IsCurrent
);

// Activity Tracking DTOs
public record UserActivityResponse(
    Guid Id,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Details,
    DateTime Timestamp,
    string? IpAddress,
    ActivityRiskLevel RiskLevel,
    bool RequiresReview,
    bool IsReviewed
);

public record UserActivitySearchRequest(
    string? Action = null,
    string? EntityType = null,
    ActivityRiskLevel? RiskLevel = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool? RequiresReview = null,
    string? SortBy = "Timestamp",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20
);

// Login History DTOs
public record UserLoginHistoryResponse(
    Guid Id,
    DateTime LoginAt,
    DateTime? LogoutAt,
    LoginResult Result,
    string? FailureReason,
    string? IpAddress,
    string? Country,
    string? City,
    string? DeviceFingerprint,
    bool IsNewLocation,
    bool IsNewDevice,
    bool IsSuspicious,
    RiskLevel RiskScore,
    bool TwoFactorUsed,
    string? TwoFactorMethod
);

public record LoginAttemptRequest(
    Guid UserId,
    LoginResult Result,
    string? FailureReason = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? Country = null,
    string? City = null,
    bool TwoFactorUsed = false,
    string? TwoFactorMethod = null
);

// Statistics DTOs
public record UserStatisticsResponse(
    Guid UserId,
    int TotalUrlsCreated,
    int TotalClicks,
    int ActiveUrls,
    int ExpiredUrls,
    DateTime? LastUrlCreated,
    DateTime? LastLogin,
    int LoginCount,
    int FailedLoginAttempts,
    Dictionary<string, int> UrlsByMonth,
    Dictionary<string, int> ClicksByMonth,
    Dictionary<string, int> TopReferrers,
    Dictionary<string, int> DeviceTypes
);

public record UserStatisticsUpdateRequest(
    int? TotalUrlsCreated = null,
    int? TotalClicks = null,
    DateTime? LastUrlCreated = null
);

// Two-Factor Authentication DTOs
public record TwoFactorSetupResponse(
    string SecretKey,
    string QrCodeUrl,
    string ManualEntryCode,
    string[] BackupCodes
);

// Authorization DTOs
public record AuthorizationContext(
    Guid UserId,
    List<string> Roles,
    List<string> Permissions,
    UserStatus Status,
    SubscriptionTier Tier,
    bool IsEmailVerified,
    bool Is2FAEnabled,
    DateTime? LastActivity,
    string? IpAddress,
    Dictionary<string, object> Claims
);

// Pagination DTOs
public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// API Response DTOs
public record ApiResponse<T>(
    bool Success,
    T? Data = default,
    string? Message = null,
    List<string>? Errors = null,
    Dictionary<string, object>? Metadata = null
);

public record ApiErrorResponse(
    string Error,
    string? Message = null,
    int? Code = null,
    Dictionary<string, object>? Details = null
);

// Bulk Operations DTOs
public record BulkUserOperationRequest(
    List<Guid> UserIds,
    string Operation, // activate, deactivate, suspend, ban, delete
    string? Reason = null,
    DateTime? ExpiresAt = null
);

public record BulkUserOperationResponse(
    int TotalRequested,
    int SuccessCount,
    int FailureCount,
    List<BulkOperationResult> Results
);

public record BulkOperationResult(
    Guid UserId,
    bool Success,
    string? Error = null
);

// Role Assignment DTOs
public record RoleAssignmentRequest(
    Guid UserId,
    List<Guid> RoleIds,
    DateTime? ExpiresAt = null,
    string? Notes = null
);

public record RoleAssignmentResponse(
    Guid UserId,
    Guid RoleId,
    string RoleName,
    DateTime AssignedAt,
    Guid? AssignedBy,
    DateTime? ExpiresAt,
    bool IsActive,
    string? Notes
);

// User Import/Export DTOs
public record UserImportRequest(
    List<UserImportItem> Users,
    bool SendWelcomeEmails = false,
    bool RequireEmailVerification = true,
    List<string>? DefaultRoles = null
);

public record UserImportItem(
    string FirstName,
    string LastName,
    string Email,
    string Username,
    string? PhoneNumber = null,
    string? Company = null,
    string? JobTitle = null,
    List<string>? Roles = null
);

public record UserExportRequest(
    UserSearchRequest SearchCriteria,
    List<string> Fields,
    string Format = "csv" // csv, json, excel
);

// Security DTOs
public record SecurityAuditResponse(
    Guid UserId,
    List<SecurityIssue> Issues,
    RiskLevel OverallRiskLevel,
    DateTime LastAssessment,
    Dictionary<string, object> Recommendations
);

public record SecurityIssue(
    string Type,
    string Description,
    RiskLevel Severity,
    DateTime DetectedAt,
    bool IsResolved,
    string? Resolution = null
);