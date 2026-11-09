using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Admin;

[Collection(ApiCollection.Name)]
public class AdminDiagnosticsTests(ApiFixture api) : ApiTestBase(api)
{
    private static readonly IPAddress Peer = IPAddress.Parse("198.51.100.99");

    [Fact]
    public async Task Diagnostics_ReportTheClientAddressAndForwardedHeaders_ToASystemAdministrator()
    {
        var factory = Api.CreateFactory(new Dictionary<string, string?> { ["ForwardedHeaders:KnownProxies:0"] = Peer.ToString() }, Peer);
        var client = factory.CreateApiClient(TestTokens.Create(Guid.NewGuid(), UserRole.SystemAdmin));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/diagnostics/connection");
        request.Headers.Add("X-Forwarded-For", "203.0.113.77");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("remoteIpAddress").GetString().Should().Be("203.0.113.77");
        json.GetProperty("isHttps").GetBoolean().Should().BeTrue();
        json.GetProperty("xOriginalFor").GetString().Should().Contain(Peer.ToString());
    }

    [Fact]
    public async Task Diagnostics_ShowThePeerAddress_WhenNoProxyIsTrusted()
    {
        var client = Api.CreateFactory(null, Peer).CreateApiClient(TestTokens.Create(Guid.NewGuid(), UserRole.SystemAdmin));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/diagnostics/connection");
        request.Headers.Add("X-Forwarded-For", "203.0.113.77");

        var json = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("remoteIpAddress").GetString().Should().Be(Peer.ToString());
        json.GetProperty("xForwardedFor").GetString().Should().Be("203.0.113.77");
    }

    [Fact]
    public async Task Diagnostics_AreSystemAdminOnly()
    {
        var user = await Seed.CreateUserAsync();

        (await As(user).GetAsync("/api/admin/diagnostics/connection")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await As(Guid.NewGuid(), UserRole.SuperAdmin).GetAsync("/api/admin/diagnostics/connection")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Anonymous().GetAsync("/api/admin/diagnostics/connection")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}