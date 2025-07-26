using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Infrastructure.DbContexts;
using Shortly.Domain.Enums;

namespace Shortly.Infrastructure.Repositories;

internal class UserRepository(SQLServerDbContext dbContext) : IUserRepository
{
    private readonly SQLServerDbContext _dbContext = dbContext;

    #region Basic CRUD Operations
    
    public async Task<User?> AddUser(User user)
    {
        var entity = await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return entity.Entity;
    }

    public async Task<User?> UpdateUser(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUser(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;
        
        _dbContext.Users.Remove(user);
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<User?> GetUserById(Guid userId)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.ShortUrls)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(user => user.Id == userId);
    }

    public async Task<User?> GetUserByEmail(string? email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<User?> GetUserByUsername(string? username)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Username == username);
    }

    public async Task<User?> GetUserByEmailAndPassword(string? email, string? password)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email && user.PasswordHash == password);
    }
    
    public async Task<bool> IsEmailOrUsernameTaken(string email, string username)
    {
        return await _dbContext.Users
            .AnyAsync(user => user.Email == email || user.Username == username);
    }

    #endregion

    #region Authentication and Security

    public async Task<bool> VerifyPassword(Guid userId, string password)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        return user != null && user.PasswordHash == password; // In real implementation, use proper password hashing
    }

    public async Task<bool> UpdatePassword(Guid userId, string passwordHash)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateLastLogin(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> IncrementFailedLoginAttempts(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.FailedLoginAttempts++;
        user.UpdatedAt = DateTime.UtcNow;
        
        // Lock account after 5 failed attempts for 30 minutes
        if (user.FailedLoginAttempts >= 5)
        {
            user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
        }
        
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ResetFailedLoginAttempts(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> LockUser(Guid userId, DateTime? lockUntil)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.LockedUntil = lockUntil;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> UnlockUser(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> IsUserLocked(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null) return false;
        
        return user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow;
    }

    #endregion

    #region Email Verification

    public async Task<bool> MarkEmailAsVerified(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.EmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> IsEmailVerified(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        return user?.EmailVerified ?? false;
    }

    #endregion

    #region Two-Factor Authentication

    public async Task<bool> EnableTwoFactor(Guid userId, string secret)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.TwoFactorEnabled = true;
        user.TwoFactorSecret = secret;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> DisableTwoFactor(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<string?> GetTwoFactorSecret(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        return user?.TwoFactorSecret;
    }

    #endregion

    #region Profile Management

    public async Task<bool> UpdateProfile(Guid userId, string name, string? timeZone, string? profilePictureUrl)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.Name = name;
        user.TimeZone = timeZone;
        user.ProfilePictureUrl = profilePictureUrl;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateSubscriptionPlan(Guid userId, enSubscriptionPlan plan)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.SubscriptionPlan = plan;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateUserRole(Guid userId, enUserRole role)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ActivateUser(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeactivateUser(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    #endregion

    #region Usage Tracking

    public async Task<bool> IncrementLinksCreated(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        // Reset monthly usage if it's a new month
        if (DateTime.UtcNow >= user.MonthlyResetDate)
        {
            user.MonthlyLinksCreated = 0;
            user.MonthlyResetDate = DateTime.UtcNow.AddMonths(1);
        }

        user.MonthlyLinksCreated++;
        user.TotalLinksCreated++;
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> ResetMonthlyUsage(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return false;

        user.MonthlyLinksCreated = 0;
        user.MonthlyResetDate = DateTime.UtcNow.AddMonths(1);
        user.UpdatedAt = DateTime.UtcNow;
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<int> GetMonthlyLinksCreated(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null) return 0;
        
        // Reset if it's a new month
        if (DateTime.UtcNow >= user.MonthlyResetDate)
        {
            return 0;
        }
        
        return user.MonthlyLinksCreated;
    }

    public async Task<int> GetTotalLinksCreated(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        return user?.TotalLinksCreated ?? 0;
    }

    public async Task<bool> CanCreateMoreLinks(Guid userId, int limit)
    {
        var monthlyLinks = await GetMonthlyLinksCreated(userId);
        return monthlyLinks < limit;
    }

    #endregion

    #region Advanced Queries

    public async Task<IEnumerable<User>> GetUsersByRole(enUserRole role)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == role)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersBySubscriptionPlan(enSubscriptionPlan plan)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.SubscriptionPlan == plan)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetActiveUsers()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetInactiveUsers()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => !u.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUnverifiedUsers()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => !u.EmailVerified)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetLockedUsers()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.LockedUntil.HasValue && u.LockedUntil.Value > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersWithTwoFactorEnabled()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.TwoFactorEnabled)
            .ToListAsync();
    }

    #endregion

    #region Search and Pagination

    public async Task<(IEnumerable<User> Users, int TotalCount)> SearchUsers(
        string? searchTerm,
        enUserRole? role,
        enSubscriptionPlan? subscriptionPlan,
        bool? isActive,
        bool? emailVerified,
        int page,
        int pageSize)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => 
                u.Name.Contains(searchTerm) ||
                u.Email.Contains(searchTerm) ||
                u.Username.Contains(searchTerm));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (subscriptionPlan.HasValue)
        {
            query = query.Where(u => u.SubscriptionPlan == subscriptionPlan.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (emailVerified.HasValue)
        {
            query = query.Where(u => u.EmailVerified == emailVerified.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (users, totalCount);
    }

    #endregion
}