using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Users;

namespace Shortly.Infrastructure.Repositories;

internal sealed class UserRepository(ApplicationDbContext dbContext) : 
    Repository<User, Guid>(dbContext), IUserRepository
{
   private IQueryable<User> ActiveUsers() => DbContext.Set<User>()
        .Include(u => u.Profile)
        .Include(u => u.Security)
        .Include(u => u.Usage)
        .Where(u => !u.IsDeleted);

    public override Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        ActiveUsers().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return [];

        return await DbContext.Set<User>()
            .AsSplitQuery()
            .Include(u => u.Profile)
            .Include(u => u.Security)
            .Include(u => u.Usage)
            .Where(u => ids.Contains(u.Id) && !u.IsDeleted)
            .ToListAsync(cancellationToken);
    }
    
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        ActiveUsers().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        ActiveUsers().FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken cancellationToken = default) =>
        ActiveUsers().FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.Username == emailOrUsername, cancellationToken);

    public Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default) =>
        ActiveUsers().FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        DbContext.Set<User>().AnyAsync(u => u.Id == userId, cancellationToken); 

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        DbContext.Set<User>().AnyAsync(u => u.Email == email, cancellationToken); 
    
    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) =>
        DbContext.Set<User>().AnyAsync(u => u.Username == username, cancellationToken);

    public Task<bool> EmailOrUsernameExistsAsync(string email, string username, CancellationToken cancellationToken = default) =>
        DbContext.Set<User>().AnyAsync(u => u.Email == email || u.Username == username, cancellationToken);
}