using Microsoft.EntityFrameworkCore;
using Shortly.Domain.Abstractions;

namespace Shortly.Infrastructure.Repositories;

public abstract class Repository<TEntity, TId>(ApplicationDbContext dbContext)
    where TEntity : Entity<TId>
{
    protected ApplicationDbContext DbContext { get; } = dbContext;

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await DbContext.Set<TEntity>().FirstOrDefaultAsync(e => Equals(e.Id, id), cancellationToken);

    public virtual void Add(TEntity entity) => DbContext.Set<TEntity>().Add(entity);

    public virtual void Remove(TEntity entity) => DbContext.Set<TEntity>().Remove(entity);
}

public abstract class Repository<TEntity>(ApplicationDbContext dbContext)
    : Repository<TEntity, Guid>(dbContext)
    where TEntity : Entity<Guid>;