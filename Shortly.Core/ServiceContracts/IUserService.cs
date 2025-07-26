using Shortly.Core.DTOs.UserDTOs;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.ServiceContracts;

public interface IUserService
{
    // Profile management
    Task<UserProfileDto?> GetUserProfile(Guid userId);
    Task<bool> UpdateUserProfile(Guid userId, UpdateUserProfileRequest request);
    Task<bool> DeleteUserAccount(Guid userId);
    
    // Password management
    Task<bool> ChangePassword(Guid userId, ChangePasswordRequest request);
    Task<bool> ForgotPassword(ForgotPasswordRequest request);
    Task<bool> ResetPassword(ResetPasswordRequest request);
    Task<bool> ValidateResetToken(string email, string token);
    
    // Email verification
    Task<bool> SendEmailVerification(Guid userId);
    Task<bool> VerifyEmail(EmailVerificationRequest request);
    Task<bool> ResendEmailVerification(ResendEmailVerificationRequest request);
    
    // Two-factor authentication
    Task<TwoFactorSetupResponse?> SetupTwoFactor(Guid userId);
    Task<bool> EnableTwoFactor(Guid userId, TwoFactorSetupRequest request);
    Task<bool> DisableTwoFactor(Guid userId, DisableTwoFactorRequest request);
    Task<bool> VerifyTwoFactorCode(Guid userId, string code);
    Task<string[]> GenerateBackupCodes(Guid userId);
    
    // Account security
    Task<bool> LockUserAccount(Guid userId, DateTime? lockUntil);
    Task<bool> UnlockUserAccount(Guid userId);
    Task<bool> IsAccountLocked(Guid userId);
    Task<bool> ResetFailedLoginAttempts(Guid userId);
    
    // Subscription and role management
    Task<bool> UpdateSubscriptionPlan(Guid userId, enSubscriptionPlan plan);
    Task<bool> UpdateUserRole(Guid userId, enUserRole role);
    Task<bool> ActivateUser(Guid userId);
    Task<bool> DeactivateUser(Guid userId);
    
    // Usage tracking
    Task<bool> TrackLinkCreation(Guid userId);
    Task<bool> CanCreateMoreLinks(Guid userId);
    Task<int> GetRemainingLinksForMonth(Guid userId);
    Task<bool> ResetMonthlyUsage(Guid userId);
    
    // Advanced user queries
    Task<IEnumerable<UserProfileDto>> GetUsersByRole(enUserRole role);
    Task<IEnumerable<UserProfileDto>> GetUsersBySubscriptionPlan(enSubscriptionPlan plan);
    Task<IEnumerable<UserProfileDto>> GetActiveUsers();
    Task<IEnumerable<UserProfileDto>> GetInactiveUsers();
    Task<IEnumerable<UserProfileDto>> GetUnverifiedUsers();
    Task<IEnumerable<UserProfileDto>> GetLockedUsers();
    
    // Search and pagination
    Task<(IEnumerable<UserProfileDto> Users, int TotalCount)> SearchUsers(
        string? searchTerm,
        enUserRole? role,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        bool? emailVerified,
        int page,
        int pageSize);
    
    // Account validation
    Task<bool> ValidateUserAccess(Guid userId);
    Task<bool> IsEmailTaken(string email, Guid? excludeUserId = null);
    Task<bool> IsUsernameTaken(string username, Guid? excludeUserId = null);
    
    // Bulk operations
    Task<bool> BulkActivateUsers(IEnumerable<Guid> userIds);
    Task<bool> BulkDeactivateUsers(IEnumerable<Guid> userIds);
    Task<bool> BulkUpdateSubscriptionPlan(IEnumerable<Guid> userIds, enSubscriptionPlan plan);
    
    // Analytics and reporting
    Task<int> GetTotalUsersCount();
    Task<int> GetActiveUsersCount();
    Task<int> GetUsersCountByPlan(enSubscriptionPlan plan);
    Task<int> GetUsersCountByRole(enUserRole role);
}