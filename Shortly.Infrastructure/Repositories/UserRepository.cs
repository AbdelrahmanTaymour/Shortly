using Microsoft.EntityFrameworkCore;
using Shortly.Core.DTOs.UsersDTOs;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Domain.Enums;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for user-related data operations.
/// Handles direct database interactions using Entity Framework Core.
/// </summary>
internal class UserRepository(SQLServerDbContext dbContext) : IUserRepository
{
    private readonly SQLServerDbContext _dbContext = dbContext;

    #region Admin
    
    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetAll()
    {
        return await _dbContext.Users.AsNoTracking().ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<(IEnumerable<UserViewDto> Users, int TotalCount)> SearchUsers(string? searchTerm, enUserRole? role,
        enSubscriptionPlan? subscriptionPlan, bool? isActive, int page, int pageSize)
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
            query = query.Where(u => u.Role == role.Value);
        
        if(subscriptionPlan.HasValue) 
            query = query.Where(u => u.SubscriptionPlan == subscriptionPlan.Value);
        
        if(isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);
        
        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and projection
        var users = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserViewDto(u.Id, u.Name, u.Email, u.Username, u.SubscriptionPlan,u.Role,u.IsActive))
            .ToListAsync();

        return (users, totalCount);
    }
    
    
    /// <inheritdoc/>
    public async Task<User?> GetUserById(Guid userId)
    {
        return await _dbContext.Users.FindAsync(userId);
    }
    
    
    /// <inheritdoc/>
    public async Task<User?> GetActiveUserByEmail(string? email)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive && !u.IsDeleted);
    }
    
    
    /// <inheritdoc/>
    public async Task<User?> GetActiveUserByUsername(string? username)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && !u.IsDeleted);
    }
    
    
    /// <inheritdoc/>
    public async Task<User?> GetActiveUserByEmailAndPassword(string? email, string? password)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == password
               && u.IsActive && !u.IsDeleted);
    }
    
    /// <inheritdoc/>
    public async Task<User?> AddUser(User user)
    {
        var entity = await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return entity.Entity;
    }
    
    /// <inheritdoc/>
    public async Task<User?> UpdateUser(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }
    
    /// <inheritdoc/>
    public async Task<bool> HardDeleteUser(Guid userId)
    {
        var rowAffected = await _dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync();
        return rowAffected > 0;
    }
    
    /// <inheritdoc/>
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
    
    /// <inheritdoc/>
    public async Task<bool> LockUser(Guid userId, DateTime? lockUntil)
    {
        int rowsAffected = await _dbContext.Users
            .Where(u => u.Id == userId && u.LockedUntil == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LockedUntil, lockUntil)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
            );
        return rowsAffected > 0;
    }
    
    /// <inheritdoc/>
    public async Task<bool> UnlockUser(Guid userId)
    {
        int rowsAffected = await _dbContext.Users
            .Where(u => u.Id == userId && u.LockedUntil != null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LockedUntil, (DateTime?)null)
                .SetProperty(u => u.FailedLoginAttempts, 0)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
        return rowsAffected > 0;
    }
    
    /// <inheritdoc/>
    public async Task<bool> ActivateUser(Guid userId)=> await SetUserActiveStatus(userId, true);
    
    /// <inheritdoc/>
    public async Task<bool> DeactivateUser(Guid userId) => await SetUserActiveStatus(userId, false);

    /// <inheritdoc/>
    public async Task<UserAvailabilityInfo?> GetUserAvailabilityInfo(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserAvailabilityInfo
                (
                    !u.IsDeleted,                 // Exists
                    u.IsActive && !u.IsDeleted, // IsActive
                    u.LockedUntil.HasValue && u.LockedUntil.Value > DateTime.UtcNow   // IsLocked
                )
            )
            .FirstOrDefaultAsync();
        return user;
    }
    
    /// <inheritdoc/>
    public async Task<bool> IsEmailOrUsernameTaken(string email, string username)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Email == email || user.Username == username);
    }
    #endregion

    
    
    #region Private Helper Methods

    private async Task<bool> SetUserActiveStatus(Guid userId, bool isActive)
    {
        var rowAffected = await _dbContext.Users
            .Where(u => u.Id == userId && u.IsActive == !isActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.IsActive, isActive)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow));
        return rowAffected > 0;
    }

    #endregion
    
}