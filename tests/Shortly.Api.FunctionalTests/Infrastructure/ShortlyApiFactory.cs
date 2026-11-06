using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Shortly.Api.FunctionalTests.Infrastructure;

/// <summary>Boots the real Shortly API (real pipeline, real EF/Dapper) against the test PostgreSQL container.</summary>
public sealed class ShortlyApiFactory : WebApplicationFactory<Program>
{
    public const string JwtKey = "functional-tests-signing-key-that-is-long-enough-for-hs256!";
    public const string JwtIssuer = "shortly-functional-tests";
    public const string JwtAudience = "shortly-functional-tests-clients";

    private readonly Dictionary<string, string?> _settings;
    private static readonly IPAddress DefaultPeer = IPAddress.Parse("192.0.2.10");
    private readonly IPAddress _remoteIpAddress;

    /// <param name="remoteIpAddress">
    /// The address the test server reports for the TCP peer. TestServer reports none by default, which Kestrel never does
    /// (and the redirect endpoints reject). Defaults to a public, untrusted address (TEST-NET-1), i.e. a direct client.
    /// Pass a different address to simulate a reverse proxy that is, or is not, configured as trusted.
    /// </param>
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

    /// <summary>Runs before the application pipeline so forwarded-header processing sees this peer address.</summary>
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
