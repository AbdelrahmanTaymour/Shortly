using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories;

internal class UserRepository(SQLServerDbContext dbContext) : IUserRepository
{
    private readonly SQLServerDbContext _dbContext = dbContext;

    #region CRUD Operations
    public async Task<User?> GetUserById(Guid userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }
    public async Task<User?> GetActiveUserByEmail(string? email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);
    }
    public async Task<User?> GetActiveUserByUsername(string? username)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && !u.IsDeleted);
    }
    public async Task<User?> GetActiveUserByEmailAndPassword(string? email, string? password)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password
               && u.IsActive && !u.IsDeleted);
    }
    public async Task<User?> AddUser(User user)
    {
        var entity = await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return entity.Entity;
    }
    public async Task<User?> UpdateUser(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<bool> HardDeleteUser(User user)
    {
        if (user == null) return false;
        _dbContext.Users.Remove(user);
        return await _dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> SoftDeleteUser(Guid userId, Guid deletedBy)
    {
        var affectedRows = await _dbContext.Users
            .Where(u => u.Id == userId && !u.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.IsDeleted, true)
                .SetProperty(u => u.DeletedAt, DateTime.UtcNow)
                .SetProperty(u => u.DeletedBy, deletedBy)
                .SetProperty(u => u.IsActive, false)
            );

        return affectedRows > 0;
    }
    
    public async Task<bool> IsEmailOrUsernameTaken(string email, string username)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email == email || user.Username == username);
    }

    public async Task<bool> IsUserActive(Guid id)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == id && user.IsActive && !user.IsDeleted);
    }

    public async Task<bool> IsUserActiveByEmail(string email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email == email && user.IsActive && !user.IsDeleted);
    }

    public async Task<bool> IsUserExists(Guid userId)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId && !user.IsDeleted);
    }
    

    #endregion
    
    #region Authentication and security
    
    public Task<bool> VerifyPassword(Guid userId, string password)
    {
        throw new NotImplementedException();
    }
    public Task<bool> UpdatePassword(Guid userId, string passwordHash)
    {
        throw new NotImplementedException();
    }
    public Task<bool> UpdateLastLogin(Guid userId)
    {
        throw new NotImplementedException();
    }
    public Task<bool> IncrementFailedLoginAttempts(Guid userId)
    {
        throw new NotImplementedException();
    }
    public Task<bool> LockUser(Guid userId, DateTime? lockUntil)
    {
        throw new NotImplementedException();
    }
    public Task<bool> UnlockUser(Guid userId)
    {
        throw new NotImplementedException();
    }
    public Task<bool> IsUserLocked(Guid userId)
    {
        throw new NotImplementedException();
    }
    
    #endregion
    
    #region Email verification

    public Task<bool> MarkEmailAsVerified(Guid userId)
    {
        throw new NotImplementedException();
    }
    public Task<bool> IsEmailVerified(Guid userId)
    {
        throw new NotImplementedException();
    }

    #endregion
    
    #region Two-factor authentication

    public Task<bool> EnableTwoFactor(Guid userId, string secret)
    {
        throw new NotImplementedException();
    }
    public Task<bool> DisableTwoFactor(Guid userId)
    {
        throw new NotImplementedException();
    }
    public Task<string?> GetTwoFactorSecret(Guid userId)
    {
        throw new NotImplementedException();
    }

    #endregion
    
    #region Profile management

    public Task<bool> UpdateProfile(Guid userId, string name, string? timeZone, string? profilePictureUrl)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateSubscriptionPlan(Guid userId, enSubscriptionPlan plan)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateUserRole(Guid userId, enUserRole role)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ActivateUser(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeactivateUser(Guid userId)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Usage tracking

    public Task<bool> IncrementLinksCreated(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ResetMonthlyUsage(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetMonthlyLinksCreated(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetTotalLinksCreated(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CanCreateMoreLinks(Guid userId, int limit)
    {
        throw new NotImplementedException();
    }
    

    #endregion
    
    #region Advanced queries

    public async Task<IEnumerable<User>> GetAll()
    {
        return await _dbContext.Users.AsNoTracking().ToListAsync();
    }
    
    public Task<IEnumerable<User>> GetUsersByRole(enUserRole role)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetUsersBySubscriptionPlan(enSubscriptionPlan plan)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetActiveUsers()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetInactiveUsers()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetUnverifiedUsers()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetLockedUsers()
    {
        throw new NotImplementedException();
    }

    #endregion
    
    #region Search and pagination

    public Task<(IEnumerable<User> Users, int TotalCount)> SearchUsers(string? searchTerm, enUserRole? role, enSubscriptionPlan? subscriptionPlan, bool? isActive,
        bool? emailVerified, int page, int pageSize)
    {
        throw new NotImplementedException();
    }

    #endregion
}