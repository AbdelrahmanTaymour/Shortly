using Shortly.Core.DTOs;
using Shortly.Core.Entities;

namespace Shortly.Core.ServiceContracts;

/// <summary>
/// Service contract for user management operations
/// </summary>
public interface IUserManagementService
{
    // User CRUD Operations
    Task<UserResponse> CreateUserAsync(CreateUserRequest request);
    Task<UserResponse?> GetUserByIdAsync(Guid userId);
    Task<UserResponse?> GetUserByEmailAsync(string email);
    Task<UserResponse?> GetUserByUsernameAsync(string username);
    Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(Guid userId, string reason);
    Task<PagedResult<UserResponse>> GetUsersAsync(UserSearchRequest request);
    
    // User Status Management
    Task<bool> ActivateUserAsync(Guid userId);
    Task<bool> DeactivateUserAsync(Guid userId, string reason);
    Task<bool> SuspendUserAsync(Guid userId, string reason, DateTime? until = null);
    Task<bool> BanUserAsync(Guid userId, string reason);
    
    // Password Management
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ConfirmPasswordResetAsync(string token, string newPassword);
    Task<bool> ForcePasswordChangeAsync(Guid userId);
    
    // Email Verification
    Task<bool> SendEmailVerificationAsync(Guid userId);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> ResendEmailVerificationAsync(string email);
    
    // Two-Factor Authentication
    Task<TwoFactorSetupResponse> SetupTwoFactorAsync(Guid userId);
    Task<bool> EnableTwoFactorAsync(Guid userId, string token);
    Task<bool> DisableTwoFactorAsync(Guid userId, string token);
    Task<string[]> GenerateRecoveryCodesAsync(Guid userId);
    
    // Account Lockout
    Task<bool> LockUserAsync(Guid userId, TimeSpan duration, string reason);
    Task<bool> UnlockUserAsync(Guid userId);
    Task<bool> ResetFailedLoginAttemptsAsync(Guid userId);
    
    // User Sessions
    Task<PagedResult<UserSessionResponse>> GetUserSessionsAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<bool> TerminateSessionAsync(Guid sessionId);
    Task<bool> TerminateAllUserSessionsAsync(Guid userId, Guid? exceptSessionId = null);
    
    // User Activity
    Task<PagedResult<UserActivityResponse>> GetUserActivityAsync(Guid userId, UserActivitySearchRequest request);
    Task LogUserActivityAsync(Guid userId, string action, string? entityType = null, string? entityId = null, object? details = null);
    
    // Login History
    Task<PagedResult<UserLoginHistoryResponse>> GetLoginHistoryAsync(Guid userId, int page = 1, int pageSize = 20);
    Task LogLoginAttemptAsync(LoginAttemptRequest request);
    
    // User Statistics
    Task<UserStatisticsResponse> GetUserStatisticsAsync(Guid userId);
    Task UpdateUserStatisticsAsync(Guid userId, UserStatisticsUpdateRequest request);
}

/// <summary>
/// Service contract for role and permission management
/// </summary>
public interface IRoleManagementService
{
    // Role Management
    Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request);
    Task<RoleResponse?> GetRoleByIdAsync(Guid roleId);
    Task<RoleResponse?> GetRoleByNameAsync(string roleName);
    Task<RoleResponse> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request);
    Task<bool> DeleteRoleAsync(Guid roleId);
    Task<List<RoleResponse>> GetAllRolesAsync();
    Task<PagedResult<RoleResponse>> GetRolesAsync(RoleSearchRequest request);
    
    // Permission Management
    Task<PermissionResponse> CreatePermissionAsync(CreatePermissionRequest request);
    Task<PermissionResponse?> GetPermissionByIdAsync(Guid permissionId);
    Task<PermissionResponse?> GetPermissionByNameAsync(string permissionName);
    Task<PermissionResponse> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request);
    Task<bool> DeletePermissionAsync(Guid permissionId);
    Task<List<PermissionResponse>> GetAllPermissionsAsync();
    Task<Dictionary<string, List<PermissionResponse>>> GetPermissionsByCategoryAsync();
    
    // Role-Permission Assignment
    Task<bool> AssignPermissionToRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId);
    Task<bool> AssignPermissionsToRoleAsync(Guid roleId, List<Guid> permissionIds);
    Task<bool> SetRolePermissionsAsync(Guid roleId, List<Guid> permissionIds);
    Task<List<PermissionResponse>> GetRolePermissionsAsync(Guid roleId);
    
    // User-Role Assignment
    Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, Guid assignedBy, DateTime? expiresAt = null, string? notes = null);
    Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId);
    Task<bool> AssignRolesToUserAsync(Guid userId, List<Guid> roleIds, Guid assignedBy);
    Task<bool> SetUserRolesAsync(Guid userId, List<Guid> roleIds, Guid assignedBy);
    Task<List<RoleResponse>> GetUserRolesAsync(Guid userId);
    Task<List<UserResponse>> GetUsersInRoleAsync(Guid roleId);
    
    // Permission Checking
    Task<bool> UserHasPermissionAsync(Guid userId, string permissionName);
    Task<bool> UserHasRoleAsync(Guid userId, string roleName);
    Task<List<string>> GetUserPermissionsAsync(Guid userId);
    Task<List<string>> GetEffectiveUserPermissionsAsync(Guid userId);
    
    // System Setup
    Task InitializeDefaultRolesAndPermissionsAsync();
    Task<bool> SeedDefaultDataAsync();
}

/// <summary>
/// Service contract for user authentication and authorization
/// </summary>
public interface IAuthorizationService
{
    // Permission Checking
    Task<bool> HasPermissionAsync(Guid userId, string permission);
    Task<bool> HasRoleAsync(Guid userId, string role);
    Task<bool> HasAnyPermissionAsync(Guid userId, params string[] permissions);
    Task<bool> HasAllPermissionsAsync(Guid userId, params string[] permissions);
    
    // Resource-based Authorization
    Task<bool> CanAccessResourceAsync(Guid userId, string resourceType, string resourceId, string action);
    Task<bool> CanManageUserAsync(Guid currentUserId, Guid targetUserId);
    Task<bool> CanAssignRoleAsync(Guid currentUserId, string roleName);
    
    // Policy-based Authorization
    Task<bool> EvaluatePolicyAsync(Guid userId, string policyName, object? resource = null);
    
    // Context-aware Authorization
    Task<AuthorizationContext> GetAuthorizationContextAsync(Guid userId);
    Task<bool> IsAuthorizedAsync(AuthorizationContext context, string requirement);
}