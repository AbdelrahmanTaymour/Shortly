using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}