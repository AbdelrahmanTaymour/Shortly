using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

public class UserAuditLogRepository(SQLServerDbContext dbContext, ILogger<UserAuditLogRepository> logger) : IUserAuditLogRepository
{
    public async Task AddAsync(UserAuditLog log)
    {
        await dbContext.UserAuditLogs.AddAsync(log);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserAuditLog>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserAuditLogs
            .AsNoTracking()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.TimeStamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserAuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<UserAuditLog>> SearchAsync(Guid? userId = null, string? action = null, DateTime? from = null, DateTime? to = null, int page = 1,
        int pageSize = 100, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UserAuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue)
            query = query.Where(log => log.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(log => log.Action.Contains(action));

        if (from.HasValue)
            query = query.Where(log => log.TimeStamp >= from.Value);

        if (to.HasValue)
            query = query.Where(log => log.TimeStamp <= to.Value);

        return await query
            .OrderByDescending(log => log.TimeStamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    }
}