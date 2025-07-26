using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shortly.Core.DTOs.UserDTOs;
using Shortly.Core.RepositoryContract;
using Shortly.Core.ServiceContracts;
using Shortly.Domain.Entities;
using Shortly.Domain.Enums;

namespace Shortly.Core.Services;

public class UserService(
    IUserRepository userRepository,
    IConfiguration configuration,
    ILogger<UserService> logger) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<UserService> _logger = logger;

    private readonly Dictionary<enSubscriptionPlan, int> _subscriptionLimits = new()
    {
        { enSubscriptionPlan.Free, 10 },
        { enSubscriptionPlan.Starter, 100 },
        { enSubscriptionPlan.Professional, 1000 },
        { enSubscriptionPlan.Enterprise, -1 } // Unlimited
    };

    #region Profile Management

    public async Task<UserProfileDto?> GetUserProfile(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return null;
            }

            return MapToUserProfileDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateUserProfile(Guid userId, UpdateUserProfileRequest request)
    {
        try
        {
            var success = await _userRepository.UpdateProfile(userId, request.Name, request.TimeZone, request.ProfilePictureUrl);
            if (success)
            {
                _logger.LogInformation("User profile updated successfully for user {UserId}", userId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteUserAccount(Guid userId)
    {
        try
        {
            var success = await _userRepository.DeleteUser(userId);
            if (success)
            {
                _logger.LogInformation("User account deleted successfully: {UserId}", userId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Password Management

    public async Task<bool> ChangePassword(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            // Verify current password
            if (!await _userRepository.VerifyPassword(userId, BCrypt.Net.BCrypt.HashPassword(request.CurrentPassword)))
            {
                _logger.LogWarning("Invalid current password attempt for user {UserId}", userId);
                return false;
            }

            // Hash new password and update
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            var success = await _userRepository.UpdatePassword(userId, hashedPassword);
            
            if (success)
            {
                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ForgotPassword(ForgotPasswordRequest request)
    {
        try
        {
            var user = await _userRepository.GetUserByEmail(request.Email);
            if (user == null)
            {
                // Don't reveal if email exists - return true for security
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
                return true;
            }

            // Generate reset token and store it (in a real implementation, store in a separate table with expiry)
            var resetToken = GenerateResetToken();
            
            // TODO: Store reset token in database with expiry (add PasswordResetToken entity)
            // TODO: Send reset email
            
            _logger.LogInformation("Password reset initiated for user {UserId}", user.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request for email {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> ResetPassword(ResetPasswordRequest request)
    {
        try
        {
            var user = await _userRepository.GetUserByEmail(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", request.Email);
                return false;
            }

            // TODO: Validate reset token from database
            if (!await ValidateResetToken(request.Email, request.ResetToken))
            {
                _logger.LogWarning("Invalid reset token for user {UserId}", user.Id);
                return false;
            }

            // Hash new password and update
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            var success = await _userRepository.UpdatePassword(user.Id, hashedPassword);
            
            if (success)
            {
                // Reset failed login attempts
                await _userRepository.ResetFailedLoginAttempts(user.Id);
                _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> ValidateResetToken(string email, string token)
    {
        try
        {
            // TODO: Implement proper token validation from database
            // This is a placeholder implementation
            return !string.IsNullOrEmpty(token) && token.Length >= 32;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token for email {Email}", email);
            return false;
        }
    }

    #endregion

    #region Email Verification

    public async Task<bool> SendEmailVerification(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null || user.EmailVerified)
            {
                return false;
            }

            // TODO: Generate verification token and send email
            var verificationToken = GenerateVerificationToken();
            
            _logger.LogInformation("Email verification sent for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email verification for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> VerifyEmail(EmailVerificationRequest request)
    {
        try
        {
            var user = await _userRepository.GetUserByEmail(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Email verification attempted for non-existent email: {Email}", request.Email);
                return false;
            }

            // TODO: Validate verification token
            if (!IsValidVerificationToken(request.VerificationToken))
            {
                _logger.LogWarning("Invalid verification token for user {UserId}", user.Id);
                return false;
            }

            var success = await _userRepository.MarkEmailAsVerified(user.Id);
            if (success)
            {
                _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for {Email}", request.Email);
            throw;
        }
    }

    public async Task<bool> ResendEmailVerification(ResendEmailVerificationRequest request)
    {
        try
        {
            var user = await _userRepository.GetUserByEmail(request.Email);
            if (user == null || user.EmailVerified)
            {
                return false;
            }

            return await SendEmailVerification(user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending email verification for {Email}", request.Email);
            throw;
        }
    }

    #endregion

    #region Two-Factor Authentication

    public async Task<TwoFactorSetupResponse?> SetupTwoFactor(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                return null;
            }

            // Generate secret for TOTP
            var secret = GenerateTwoFactorSecret();
            var appName = _configuration["App:Name"] ?? "Shortly";
            var qrCodeUri = $"otpauth://totp/{appName}:{user.Email}?secret={secret}&issuer={appName}";
            
            // Generate backup codes
            var backupCodes = GenerateBackupCodes();

            return new TwoFactorSetupResponse
            {
                QrCodeUri = qrCodeUri,
                ManualEntryKey = secret,
                BackupCodes = backupCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up two-factor authentication for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> EnableTwoFactor(Guid userId, TwoFactorSetupRequest request)
    {
        try
        {
            // TODO: Verify the code against the temporary secret
            // For now, we'll assume the code is valid if it's 6 digits
            if (request.VerificationCode.Length != 6 || !request.VerificationCode.All(char.IsDigit))
            {
                return false;
            }

            var secret = GenerateTwoFactorSecret(); // In real implementation, use the temporary secret
            var success = await _userRepository.EnableTwoFactor(userId, secret);
            
            if (success)
            {
                _logger.LogInformation("Two-factor authentication enabled for user {UserId}", userId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling two-factor authentication for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DisableTwoFactor(Guid userId, DisableTwoFactorRequest request)
    {
        try
        {
            // Verify password
            if (!await _userRepository.VerifyPassword(userId, BCrypt.Net.BCrypt.HashPassword(request.Password)))
            {
                return false;
            }

            // Verify 2FA code
            if (!await VerifyTwoFactorCode(userId, request.TwoFactorCode))
            {
                return false;
            }

            var success = await _userRepository.DisableTwoFactor(userId);
            if (success)
            {
                _logger.LogInformation("Two-factor authentication disabled for user {UserId}", userId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> VerifyTwoFactorCode(Guid userId, string code)
    {
        try
        {
            var secret = await _userRepository.GetTwoFactorSecret(userId);
            if (string.IsNullOrEmpty(secret))
            {
                return false;
            }

            // TODO: Implement proper TOTP verification
            // For now, we'll do a simple validation
            return code.Length == 6 && code.All(char.IsDigit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying two-factor code for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string[]> GenerateBackupCodes(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null || !user.TwoFactorEnabled)
            {
                return Array.Empty<string>();
            }

            var backupCodes = GenerateBackupCodes();
            // TODO: Store backup codes in database (hashed)
            
            _logger.LogInformation("Backup codes generated for user {UserId}", userId);
            return backupCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating backup codes for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Account Security

    public async Task<bool> LockUserAccount(Guid userId, DateTime? lockUntil)
    {
        try
        {
            var success = await _userRepository.LockUser(userId, lockUntil);
            if (success)
            {
                _logger.LogInformation("User account locked: {UserId} until {LockUntil}", userId, lockUntil);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user account {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UnlockUserAccount(Guid userId)
    {
        try
        {
            var success = await _userRepository.UnlockUser(userId);
            if (success)
            {
                _logger.LogInformation("User account unlocked: {UserId}", userId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user account {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsAccountLocked(Guid userId)
    {
        try
        {
            return await _userRepository.IsUserLocked(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user account is locked {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ResetFailedLoginAttempts(Guid userId)
    {
        try
        {
            return await _userRepository.ResetFailedLoginAttempts(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting failed login attempts for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Subscription and Role Management

    public async Task<bool> UpdateSubscriptionPlan(Guid userId, enSubscriptionPlan plan)
    {
        try
        {
            var success = await _userRepository.UpdateSubscriptionPlan(userId, plan);
            if (success)
            {
                _logger.LogInformation("Subscription plan updated for user {UserId} to {Plan}", userId, plan);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> UpdateUserRole(Guid userId, enUserRole role)
    {
        try
        {
            var success = await _userRepository.UpdateUserRole(userId, role);
            if (success)
            {
                _logger.LogInformation("User role updated for user {UserId} to {Role}", userId, role);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ActivateUser(Guid userId)
    {
        try
        {
            return await _userRepository.ActivateUser(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeactivateUser(Guid userId)
    {
        try
        {
            return await _userRepository.DeactivateUser(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Usage Tracking

    public async Task<bool> TrackLinkCreation(Guid userId)
    {
        try
        {
            return await _userRepository.IncrementLinksCreated(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking link creation for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CanCreateMoreLinks(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null) return false;

            var limit = _subscriptionLimits[user.SubscriptionPlan];
            if (limit == -1) return true; // Unlimited

            return await _userRepository.CanCreateMoreLinks(userId, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking link creation limit for user {UserId}", userId);
            return false;
        }
    }

    public async Task<int> GetRemainingLinksForMonth(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null) return 0;

            var limit = _subscriptionLimits[user.SubscriptionPlan];
            if (limit == -1) return int.MaxValue; // Unlimited

            var used = await _userRepository.GetMonthlyLinksCreated(userId);
            return Math.Max(0, limit - used);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remaining links for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> ResetMonthlyUsage(Guid userId)
    {
        try
        {
            return await _userRepository.ResetMonthlyUsage(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting monthly usage for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Advanced User Queries

    public async Task<IEnumerable<UserProfileDto>> GetUsersByRole(enUserRole role)
    {
        try
        {
            var users = await _userRepository.GetUsersByRole(role);
            return users.Select(MapToUserProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by role {Role}", role);
            throw;
        }
    }

    public async Task<IEnumerable<UserProfileDto>> GetUsersBySubscriptionPlan(enSubscriptionPlan plan)
    {
        try
        {
            var users = await _userRepository.GetUsersBySubscriptionPlan(plan);
            return users.Select(MapToUserProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users by subscription plan {Plan}", plan);
            throw;
        }
    }

    public async Task<IEnumerable<UserProfileDto>> GetActiveUsers()
    {
        try
        {
            var users = await _userRepository.GetActiveUsers();
            return users.Select(MapToUserProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users");
            throw;
        }
    }

    public async Task<IEnumerable<UserProfileDto>> GetInactiveUsers()
    {
        try
        {
            var users = await _userRepository.GetInactiveUsers();
            return users.Select(MapToUserProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inactive users");
            throw;
        }
    }

    public async Task<IEnumerable<UserProfileDto>> GetUnverifiedUsers()
    {
        try
        {
            var users = await _userRepository.GetUnverifiedUsers();
            return users.Select(MapToUserProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unverified users");
            throw;
        }
    }

    public async Task<IEnumerable<UserProfileDto>> GetLockedUsers()
    {
        try
        {
            var users = await _userRepository.GetLockedUsers();
            return users.Select(MapToUserProfileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locked users");
            throw;
        }
    }

    #endregion

    #region Search and Pagination

    public async Task<(IEnumerable<UserProfileDto> Users, int TotalCount)> SearchUsers(
        string? searchTerm,
        enUserRole? role,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        bool? emailVerified,
        int page,
        int pageSize)
    {
        try
        {
            var (users, totalCount) = await _userRepository.SearchUsers(
                searchTerm, role, subscriptionPlan, isActive, emailVerified, page, pageSize);
            
            var userDtos = users.Select(MapToUserProfileDto);
            return (userDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            throw;
        }
    }

    #endregion

    #region Account Validation

    public async Task<bool> ValidateUserAccess(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null) return false;
            
            return user.IsActive && !await IsAccountLocked(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user access for {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> IsEmailTaken(string email, Guid? excludeUserId = null)
    {
        try
        {
            var user = await _userRepository.GetUserByEmail(email);
            return user != null && (excludeUserId == null || user.Id != excludeUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if email is taken: {Email}", email);
            return false;
        }
    }

    public async Task<bool> IsUsernameTaken(string username, Guid? excludeUserId = null)
    {
        try
        {
            var user = await _userRepository.GetUserByUsername(username);
            return user != null && (excludeUserId == null || user.Id != excludeUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if username is taken: {Username}", username);
            return false;
        }
    }

    #endregion

    #region Bulk Operations

    public async Task<bool> BulkActivateUsers(IEnumerable<Guid> userIds)
    {
        try
        {
            var tasks = userIds.Select(id => _userRepository.ActivateUser(id));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk activating users");
            throw;
        }
    }

    public async Task<bool> BulkDeactivateUsers(IEnumerable<Guid> userIds)
    {
        try
        {
            var tasks = userIds.Select(id => _userRepository.DeactivateUser(id));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deactivating users");
            throw;
        }
    }

    public async Task<bool> BulkUpdateSubscriptionPlan(IEnumerable<Guid> userIds, enSubscriptionPlan plan)
    {
        try
        {
            var tasks = userIds.Select(id => _userRepository.UpdateSubscriptionPlan(id, plan));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating subscription plans");
            throw;
        }
    }

    #endregion

    #region Analytics and Reporting

    public async Task<int> GetTotalUsersCount()
    {
        try
        {
            var (_, totalCount) = await _userRepository.SearchUsers(null, null, null, null, null, 1, 1);
            return totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total users count");
            throw;
        }
    }

    public async Task<int> GetActiveUsersCount()
    {
        try
        {
            var users = await _userRepository.GetActiveUsers();
            return users.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active users count");
            throw;
        }
    }

    public async Task<int> GetUsersCountByPlan(enSubscriptionPlan plan)
    {
        try
        {
            var users = await _userRepository.GetUsersBySubscriptionPlan(plan);
            return users.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users count by plan {Plan}", plan);
            throw;
        }
    }

    public async Task<int> GetUsersCountByRole(enUserRole role)
    {
        try
        {
            var users = await _userRepository.GetUsersByRole(role);
            return users.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users count by role {Role}", role);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    private static UserProfileDto MapToUserProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Username = user.Username,
            SubscriptionPlan = user.SubscriptionPlan,
            Role = user.Role,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            LastLoginAt = user.LastLoginAt,
            TimeZone = user.TimeZone,
            ProfilePictureUrl = user.ProfilePictureUrl,
            MonthlyLinksCreated = user.MonthlyLinksCreated,
            TotalLinksCreated = user.TotalLinksCreated,
            MonthlyResetDate = user.MonthlyResetDate,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private static string GenerateResetToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string GenerateVerificationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static bool IsValidVerificationToken(string token)
    {
        // TODO: Implement proper token validation
        return !string.IsNullOrEmpty(token) && token.Length >= 32;
    }

    private static string GenerateTwoFactorSecret()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static string[] GenerateBackupCodes()
    {
        var codes = new string[8];
        var random = new Random();
        
        for (int i = 0; i < codes.Length; i++)
        {
            codes[i] = random.Next(100000, 999999).ToString();
        }
        
        return codes;
    }

    #endregion
}