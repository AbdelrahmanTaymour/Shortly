using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Shortly.Core.Exceptions;
using Shortly.Domain.Abstractions;

namespace Shortly.Infrastructure;

public sealed class ApplicationDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                               { SqlState: PostgresErrorCodes.UniqueViolation } pgEx)
        {
            throw new UniqueConstraintViolationException(pgEx.ConstraintName ?? string.Empty);
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var dbTransaction = await Database.BeginTransactionAsync(cancellationToken);
        return new DbContextTransactionAdapter(dbTransaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private sealed class DbContextTransactionAdapter(IDbContextTransaction transaction) : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => 
            transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) => 
            transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}