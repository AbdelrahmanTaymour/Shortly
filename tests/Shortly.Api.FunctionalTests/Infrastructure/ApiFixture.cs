using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Infrastructure;
using Testcontainers.PostgreSql;

namespace Shortly.Api.FunctionalTests.Infrastructure;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18").Build();
    private readonly List<ShortlyApiFactory> _extraFactories = [];

    public ShortlyApiFactory Default { get; private set; } = null!;
    public Seeder Seed { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        Default = new ShortlyApiFactory(_database.GetConnectionString(), new Dictionary<string, string?>
        {
            ["RateLimiting:AnonymousShortUrlCreation:PermitLimit"] = "10000",
            ["RateLimiting:Registration:PermitLimit"] = "10000",
            ["RateLimiting:AccountEmail:PermitLimit"] = "10000",
            ["RateLimiting:ShortCodeAvailability:PermitLimit"] = "10000",
            ["RateLimiting:Login:PermitLimit"] = "10000"
        });

        await using (var scope = Default.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        }

        Seed = new Seeder(Default.Services);
    }

    public ShortlyApiFactory CreateFactory(
        IDictionary<string, string?>? overrides = null, System.Net.IPAddress? remoteIpAddress = null)
    {
        var factory = new ShortlyApiFactory(_database.GetConnectionString(), overrides, remoteIpAddress);
        _ = factory.Server;
        _extraFactories.Add(factory);
        return factory;
    }

    public async Task DisposeAsync()
    {
        foreach (var factory in _extraFactories) 
        {
            await factory.DisposeAsync();
        }
        
        await Default.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
