using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shortly.Api;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Infrastructure;
using Testcontainers.PostgreSql;

namespace Shortly.IntegrationTests.Infrastructure;

/// <summary>
/// The real Shortly application, in memory, against one throwaway PostgreSQL container.
/// The same instance is shared by every test in the project (see <see cref="IntegrationCollection"/>).
/// </summary>
public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = CreateContainer();
    private readonly CapturingEmailNotifier _emails = new();

    public CapturingEmailNotifier Emails => _emails;

    private static PostgreSqlContainer CreateContainer()
    {
        var builder = new PostgreSqlBuilder("postgres:18");

        // Optional: keep the container alive between local runs (faster start). Off by default so CI always starts clean.
        if (Environment.GetEnvironmentVariable("SHORTLY_TESTS_REUSE_DB") == "1")
            builder = builder.WithReuse(true).WithLabel("shortly-integration-tests", "postgres");

        return builder.Build();
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        // The API only auto-migrates in Development, so the test database is migrated explicitly, once.
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // A fresh, empty file keeps the GeoLite2 updater from trying to download a database.
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
            ["AppSettings:BaseUIUrl"] = "http://localhost:3000"
        };

        // UseSetting is visible to code that reads configuration eagerly while services are registered.
        foreach (var (key, value) in settings) builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            // "Who is calling" is chosen by the test, not read from an HTTP request.
            services.RemoveAll<IUserContext>();
            services.AddScoped<TestUserContext>();
            services.AddScoped<IUserContext>(sp => sp.GetRequiredService<TestUserContext>());

            services.RemoveAll<IEmailNotifier>();
            services.AddSingleton<IEmailNotifier>(_emails);
        });
    }

    public new async Task DisposeAsync()
    {
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
