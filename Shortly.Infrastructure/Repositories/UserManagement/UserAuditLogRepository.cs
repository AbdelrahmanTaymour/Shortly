using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shortly.Core.RepositoryContract.UserManagement;
using Shortly.Domain.Entities;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Repositories.UserManagement;

/// <summary>
/// SQL Server implementation of the user audit log repository.
/// </summary>
/// <remarks>
/// Uses Entity Framework Core with SQL Server for data access.
/// </remarks>
public class UserAuditLogRepository(SQLServerDbContext dbContext, ILogger<UserAuditLogRepository> logger) : IUserAuditLogRepository
{
    /// <inheritdoc/>
    public async Task AddAsync(UserAuditLog log)
    {
        await dbContext.UserAuditLogs.AddAsync(log);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserAuditLog>> GetByUserIdAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserAuditLogs
            .AsNoTracking()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.TimeStamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserAuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserAuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
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