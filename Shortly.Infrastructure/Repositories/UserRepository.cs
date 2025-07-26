using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Entities;
using Shortly.Core.RepositoryContract;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories;

internal class UserRepository(SQLServerDbContext dbContext) : IUserRepository
{
    private readonly SQLServerDbContext _dbContext = dbContext;

    public async Task<User?> AddUser(User user)
    {
        var entity = await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return entity.Entity;
    }

    public async Task<User?> GetUserByEmailAndPassword(string? email, string? password)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email && user.PasswordHash == password);
    }
    
    public async Task<bool> IsEmailOrUsernameTaken(string email, string username)
    {
        return await _dbContext.Users.AnyAsync(user => user.Email == email || user.Username == username);
    }

    public async Task<User?> GetUserByEmail(string? email)
    {
        return await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<User?> GetUserByUsername(string? username)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Username == username);
    }
}