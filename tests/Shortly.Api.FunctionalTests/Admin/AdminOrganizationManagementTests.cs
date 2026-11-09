using System.Net;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Admin;

[Collection(ApiCollection.Name)]
public class AdminOrganizationManagementTests(ApiFixture api) : ApiTestBase(api)
{
    private HttpClient SystemAdmin() => As(Guid.NewGuid(), UserRole.SystemAdmin);

    // ---- search --------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Search_FindsAnOrganizationByNameFragment()
    {
        var unique = Guid.NewGuid().ToString("N")[..12];
        var ownerId = await Seed.CreateUserAsync();
        var organizationId = await Seed.CreateOrganizationAsync(ownerId, name: $"Acme-{unique} Rockets");

        var response = await SystemAdmin().GetAsync($"/api/admin/organizations/search?search={unique}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await ReadJsonAsync(response);
        body.GetProperty("items").EnumerateArray()
            .Select(e => e.GetProperty("id").GetGuid())
            .Should().Contain(organizationId);
    }

    [Fact]
    public async Task Search_IsSystemAdminOnly()
    {
        var caller = As(await Seed.CreateUserAsync());

        (await caller.GetAsync("/api/admin/organizations/search")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Anonymous().GetAsync("/api/admin/organizations/search")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- reset usage (single) -------------------------------------------------------------------------------------

    [Fact]
    public async Task ResetUsage_ZeroesTheOrganizationsMonthlyCount()
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationMonthlyLinksAsync(org.OrganizationId, 9);

        var response = await SystemAdmin().PostAsync($"/api/admin/organizations/{org.OrganizationId}/usage/reset", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Seed.GetOrganizationMonthlyLinksAsync(org.OrganizationId)).Should().Be(0);
    }

    [Fact]
    public async Task ResetUsage_ForAnUnknownOrganization_Returns404()
    {
        var response = await SystemAdmin().PostAsync($"/api/admin/organizations/{Guid.NewGuid()}/usage/reset", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetUsage_IsAdminOnly()
    {
        var org = await CreateOrganizationAsync();

        var response = await As(org.OwnerId).PostAsync($"/api/admin/organizations/{org.OrganizationId}/usage/reset", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- reset usage (all) ----------------------------------------------------------------------------------------

    [Fact]
    public async Task ResetAllUsage_ZeroesEveryOrganizationsMonthlyCount_AndReportsHowMany()
    {
        var a = await CreateOrganizationAsync();
        var b = await CreateOrganizationAsync();
        await Seed.SetOrganizationMonthlyLinksAsync(a.OrganizationId, 2);
        await Seed.SetOrganizationMonthlyLinksAsync(b.OrganizationId, 5);
        await Seed.SetOrganizationUsageResetDateAsync(a.OrganizationId, DateTime.UtcNow.AddDays(-1));
        await Seed.SetOrganizationUsageResetDateAsync(b.OrganizationId, DateTime.UtcNow.AddDays(-1));

        var response = await SystemAdmin().PostAsync("/api/admin/organizations/usage/reset-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ReadJsonAsync(response)).GetProperty("organizationsAffected").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        (await Seed.GetOrganizationMonthlyLinksAsync(a.OrganizationId)).Should().Be(0);
        (await Seed.GetOrganizationMonthlyLinksAsync(b.OrganizationId)).Should().Be(0);
    }

    [Fact]
    public async Task ResetAllUsage_IsAdminOnly()
    {
        var caller = As(await Seed.CreateUserAsync());

        (await caller.PostAsync("/api/admin/organizations/usage/reset-all", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}