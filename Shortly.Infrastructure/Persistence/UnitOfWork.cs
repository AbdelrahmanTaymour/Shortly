using Shortly.Core.Abstractions;
using Shortly.Infrastructure.DbContexts;

namespace Shortly.Infrastructure.Persistence;

public class UnitOfWork(SqlServerDbContext dbContext): IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}