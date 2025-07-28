using Shortly.Domain.Entities;

namespace Shortly.Core.RepositoryContract;

public interface IUserRepository
{
    Task<User?> GetUserById(Guid userId);
    Task<User?> GetActiveUserByEmail(string? email);
    Task<User?> GetActiveUserByUsername(string? username);
    Task<User?> GetActiveUserByEmailAndPassword(string? email, string? password);
    Task<User?> AddUser(User user);
    Task<User?> UpdateUser(User user);
    Task<bool> HardDeleteUser(User user);
    Task<bool> SoftDeleteUser(Guid userId, Guid deletedBy);
    Task<bool> IsEmailOrUsernameTaken(string email, string username);
    Task<bool> IsUserActive(Guid id);
    Task<bool> IsUserActiveByEmail(string email);
    
    // Authentication and security
    Task<bool> VerifyPassword(Guid userId, string password);
    Task<bool> UpdatePassword(Guid userId, string passwordHash);
    Task<bool> UpdateLastLogin(Guid userId);
    Task<bool> IncrementFailedLoginAttempts(Guid userId);
    Task<bool> LockUser(Guid userId, DateTime? lockUntil);
    Task<bool> UnlockUser(Guid userId);
    Task<bool> IsUserLocked(Guid userId);
    Task<bool> IsUserExists(Guid userId);
    
    // Email verification
    Task<bool> MarkEmailAsVerified(Guid userId);
    Task<bool> IsEmailVerified(Guid userId);
    
    // Two-factor authentication
    Task<bool> EnableTwoFactor(Guid userId, string secret);
    Task<bool> DisableTwoFactor(Guid userId);
    Task<string?> GetTwoFactorSecret(Guid userId);
    
    // Profile management
    Task<bool> UpdateProfile(Guid userId, string name, string? timeZone, string? profilePictureUrl);
    Task<bool> UpdateSubscriptionPlan(Guid userId, Domain.Enums.enSubscriptionPlan plan);
    Task<bool> UpdateUserRole(Guid userId, Domain.Enums.enUserRole role);
    Task<bool> ActivateUser(Guid userId);
    Task<bool> DeactivateUser(Guid userId);
    
    // Usage tracking
    Task<bool> IncrementLinksCreated(Guid userId);
    Task<bool> ResetMonthlyUsage(Guid userId);
    Task<int> GetMonthlyLinksCreated(Guid userId);
    Task<int> GetTotalLinksCreated(Guid userId);
    Task<bool> CanCreateMoreLinks(Guid userId, int limit);
    
    // Advanced queries
    Task<IEnumerable<User>> GetAll();
    Task<IEnumerable<User>> GetUsersByRole(Domain.Enums.enUserRole role);
    Task<IEnumerable<User>> GetUsersBySubscriptionPlan(Domain.Enums.enSubscriptionPlan plan);
    Task<IEnumerable<User>> GetActiveUsers();
    Task<IEnumerable<User>> GetInactiveUsers();
    Task<IEnumerable<User>> GetUnverifiedUsers();
    Task<IEnumerable<User>> GetLockedUsers();
    
    // Search and pagination
    Task<(IEnumerable<User> Users, int TotalCount)> SearchUsers(
        string? searchTerm, 
        Domain.Enums.enUserRole? role, 
        Domain.Enums.enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        bool? emailVerified,
        int page, 
        int pageSize);
    
}