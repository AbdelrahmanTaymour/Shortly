using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Shortly.Api.FunctionalTests.Infrastructure;

/// <summary>
/// Boots the real Shortly API pipeline (including EF Core, Dapper, and background services) 
/// against the test PostgreSQL container with custom configuration and remote IP injection.
/// </summary>
public sealed class ShortlyApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "functional-tests-signing-key-that-is-long-enough-for-hs256!";
    public const string JwtIssuer = "shortly-functional-tests";
    public const string JwtAudience = "shortly-functional-tests-clients";

    private readonly Dictionary<string, string?> _settings;
    private static readonly IPAddress DefaultPeer = IPAddress.Parse("192.0.2.10");
    private readonly IPAddress _remoteIpAddress;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortlyApiFactory"/> class with connection strings, 
    /// configuration overrides, and an optional remote IP address for network simulation.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL test container connection string.</param>
    /// <param name="overrides">Optional configuration key-value pairs to override defaults.</param>
    /// <param name="remoteIpAddress">The simulated TCP peer address for the TestServer client.</param>
    public ShortlyApiFactory(
        string connectionString, IDictionary<string, string?>? overrides = null, IPAddress? remoteIpAddress = null)
    {
        _remoteIpAddress = remoteIpAddress ?? DefaultPeer;
        // A fresh, empty file keeps the GeoLite2 updater from trying to download a database from MaxMind.
        var geoDatabase = Path.Combine(Path.GetTempPath(), $"shortly-tests-geo-{Guid.NewGuid():N}.mmdb");
        File.WriteAllBytes(geoDatabase, [0]);

        _settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = connectionString,
            ["Jwt:Key"] = JwtKey,
            ["Jwt:Issuer"] = JwtIssuer,
            ["Jwt:Audience"] = JwtAudience,
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "15",
            ["Authentication:Google:ClientId"] = "test-client-id",
            ["Authentication:Google:ClientSecret"] = "test-client-secret",
            ["GeoLite2:DatabasePath"] = geoDatabase,
            ["AppSettings:BaseUIUrl"] = "http://localhost:3000"
        };

        if (overrides is null) return;
        foreach (var (key, value) in overrides) _settings[key] = value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting is visible to code that reads configuration eagerly while services are being registered.
        foreach (var (key, value) in _settings) builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
            services.AddSingleton<IStartupFilter>(new FixedRemoteIpStartupFilter(_remoteIpAddress)));
    }

    /// <summary>
    /// Runs before the application pipeline so forwarded-header and rate-limiting middleware 
    /// process the specified test peer address correctly.
    /// </summary>
    private sealed class FixedRemoteIpStartupFilter(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;
                return nextMiddleware();
            });
            next(app);
        };
    }

    /// <summary>
    /// Creates a pre-configured HTTP client for functional API testing with required user-agents 
    /// and optional JWT bearer authentication.
    /// </summary>
    public HttpClient CreateApiClient(string? bearerToken = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Real browsers always send a User-Agent, and the redirect endpoints require one.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Shortly-FunctionalTests/1.0");

        if (bearerToken is not null)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

        return client;
    }
}
