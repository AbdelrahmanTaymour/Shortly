namespace Shortly.Domain.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes concurrent operations that use the same key wait for each other until the current transaction ends.
    /// Use it around "check a limit, then insert" so two requests cannot both pass the check. Call it inside a transaction.
    /// </summary>
    Task LockAsync(string key, CancellationToken cancellationToken = default);
}