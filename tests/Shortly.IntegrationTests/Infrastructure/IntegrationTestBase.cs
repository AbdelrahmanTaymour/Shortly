using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Infrastructure;

namespace Shortly.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for every integration test. Requests go through the real MediatR pipeline, handlers, repositories and
/// database, exactly one level below the controllers.
/// </summary>
[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase(IntegrationTestWebAppFactory factory)
{
    protected IntegrationTestWebAppFactory Factory { get; } = factory;

    /// <summary>Arranges data directly in the database.</summary>
    protected TestData Data { get; } = new(factory);

    /// <summary>The caller applied to every request. Defaults to anonymous; change it with <see cref="As"/>.</summary>
    protected Caller CurrentCaller { get; private set; } = Caller.Anonymous;

    protected CapturingEmailNotifier Emails => Factory.Emails;

    protected void As(Caller caller) => CurrentCaller = caller;

    protected void As(Guid userId, Guid? organizationId = null) => CurrentCaller = Caller.User(userId, organizationId);

    /// <summary>
    /// Sends a request the way an HTTP request would: in its own DI scope, so nothing is tracked or cached between
    /// two calls of the same test.
    /// </summary>
    protected async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestUserContext>().Caller = CurrentCaller;
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    /// <summary>Runs arrangement or assertion code against the real database in a separate, untracked scope.</summary>
    protected async Task<T> Query<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return await query(db);
    }

    /// <summary>Writes arranged data directly, bypassing the application.</summary>
    protected async Task Seed(Func<ApplicationDbContext, Task> arrange)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await arrange(db);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Runs code against a real repository (resolved by its interface, in its own scope) and saves through the real unit of work,
    /// the way a handler would.
    /// </summary>
    protected async Task<TResult> WithRepository<TRepository, TResult>(
        Func<TRepository, Task<TResult>> action, bool save = true) where TRepository : notnull
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var result = await action(scope.ServiceProvider.GetRequiredService<TRepository>());
        if (save) await scope.ServiceProvider.GetRequiredService<Shortly.Domain.Abstractions.IUnitOfWork>().SaveChangesAsync();
        return result;
    }

    protected Task<TResult> WithRepository<TRepository, TResult>(Func<TRepository, TResult> action, bool save = true)
        where TRepository : notnull =>
        WithRepository<TRepository, TResult>(r => Task.FromResult(action(r)), save);
}
