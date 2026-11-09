using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Infrastructure;

namespace Shortly.IntegrationTests.Infrastructure;

[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    protected IntegrationTestWebAppFactory Factory { get; } = factory;

    protected TestData Data { get; } = new(factory);

    protected Caller CurrentCaller { get; private set; } = Caller.Anonymous;

    protected CapturingEmailNotifier Emails => Factory.Emails;

    public Task InitializeAsync()
    {
        CurrentCaller = Caller.Anonymous;
        return Factory.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected void As(Caller caller) => CurrentCaller = caller;

    protected void As(Guid userId, Guid? organizationId = null) => CurrentCaller = Caller.User(userId, organizationId);

    protected async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestUserContext>().Caller = CurrentCaller;
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    protected async Task<T> Query<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return await query(db);
    }

    protected async Task Seed(Func<ApplicationDbContext, Task> arrange)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await arrange(db);
        await db.SaveChangesAsync();
    }

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
