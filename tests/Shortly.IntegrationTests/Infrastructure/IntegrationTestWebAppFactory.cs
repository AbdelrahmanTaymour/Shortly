using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Shortly.Api;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Infrastructure;
using Testcontainers.PostgreSql;

namespace Shortly.IntegrationTests.Infrastructure;


public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = CreateContainer();
    private readonly CapturingEmailNotifier _emails = new();
 
    private NpgsqlConnection _respawnConnection = null!;
    private Respawner _respawner = null!;
 
    public CapturingEmailNotifier Emails => _emails;
 
    private static PostgreSqlContainer CreateContainer()
    {
        var builder = new PostgreSqlBuilder("postgres:18");
 
        if (Environment.GetEnvironmentVariable("SHORTLY_TESTS_REUSE_DB") == "1")
            builder = builder.WithReuse(true).WithLabel("shortly-integration-tests", "postgres");
 
        return builder.Build();
    }
 
    public async Task InitializeAsync()
    {
        await _database.StartAsync();
 
        using (var scope = Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        }
 
        _respawnConnection = new NpgsqlConnection(_database.GetConnectionString());
        await _respawnConnection.OpenAsync();
 
        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["__EFMigrationsHistory", "subscription_plans"]
        });
    }
 
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
 
        var geoDatabase = Path.Combine(Path.GetTempPath(), $"shortly-integration-geo-{Guid.NewGuid():N}.mmdb");
        File.WriteAllBytes(geoDatabase, [0]);
 
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = _database.GetConnectionString(),
            ["Jwt:Key"] = "integration-tests-signing-key-that-is-long-enough-for-hs256!",
            ["Jwt:Issuer"] = "shortly-integration-tests",
            ["Jwt:Audience"] = "shortly-integration-tests-clients",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "15",
            ["Authentication:Google:ClientId"] = "test-client-id",
            ["Authentication:Google:ClientSecret"] = "test-client-secret",
            ["GeoLite2:DatabasePath"] = geoDatabase,
            ["AppSettings:BaseUIUrl"] = "http://localhost:3000",
            ["BackgroundServices:Email"] = "false",
            ["BackgroundServices:GeoDatabaseUpdater"] = "false",
            ["BackgroundServices:ScheduledJobs"] = "false"
        };
 
        foreach (var (key, value) in settings) builder.UseSetting(key, value);
 
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserContext>();
            services.AddScoped<TestUserContext>();
            services.AddScoped<IUserContext>(sp => sp.GetRequiredService<TestUserContext>());
 
            services.RemoveAll<IEmailNotifier>();
            services.AddSingleton<IEmailNotifier>(_emails);
        });
    }
 
    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(_respawnConnection);
        _emails.Clear();
    }
 
    public new async Task DisposeAsync()
    {
        await _respawnConnection.DisposeAsync();
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
 
/// <summary>One container and one host for the whole project; tests run one after another and use unique data.</summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    public const string Name = "integration";
}
