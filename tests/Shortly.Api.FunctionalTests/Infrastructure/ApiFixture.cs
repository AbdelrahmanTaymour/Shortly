using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Infrastructure;
using Testcontainers.PostgreSql;

namespace Shortly.Api.FunctionalTests.Infrastructure;

/// <summary>One PostgreSQL container and one API host shared by every test in the "api" collection.</summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18").Build();
    private readonly List<ShortlyApiFactory> _extraFactories = [];

    public ShortlyApiFactory Default { get; private set; } = null!;
    public Seeder Seed { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // Anonymous creation is limited per IP, and every in-process test request shares one (null) IP.
        // The shared host uses a high limit so unrelated tests never trip it; rate-limit tests use their own host.
        Default = new ShortlyApiFactory(_database.GetConnectionString(), new Dictionary<string, string?>
        {
            ["RateLimiting:AnonymousShortUrlCreation:PermitLimit"] = "10000",
            ["RateLimiting:Registration:PermitLimit"] = "10000",
            ["RateLimiting:AccountEmail:PermitLimit"] = "10000",
            ["RateLimiting:ShortCodeAvailability:PermitLimit"] = "10000",
            ["RateLimiting:Login:PermitLimit"] = "10000"
        });

        // The API only auto-migrates in Development, so the test database is initialized explicitly.
        using (var scope = Default.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

        Seed = new Seeder(Default.Services);
    }

    /// <summary>A separate host with different configuration, against the same database.</summary>
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
        foreach (var factory in _extraFactories) await factory.DisposeAsync();
        await Default.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
