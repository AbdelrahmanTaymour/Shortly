using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests;

/// <summary>Account deletion and admin actions: organization ownership, self-targeting and the lock audit trail.</summary>
[Collection(ApiCollection.Name)]
public class AccountAndAdminRulesTests(ApiFixture api) : ApiTestBase(api)
{
    private HttpClient SystemAdmin(Guid? id = null) => As(id ?? Guid.NewGuid(), UserRole.SystemAdmin);

    [Fact]
    public async Task DeleteAccount_IsRefused_WhileTheUserOwnsAnOrganization()
    {
        var org = await CreateOrganizationAsync();

        var response = await As(org.OwnerId).DeleteAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("OwnsOrganizations");
    }

    [Fact]
    public async Task DeleteAccount_Succeeds_ForAUserWithoutOrganizations()
    {
        var user = await Seed.CreateUserAsync();

        (await As(user).DeleteAsync("/api/me")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task HardDelete_IsRefused_ForAnOrganizationOwner_AndForTheAdminThemselves()
    {
        var org = await CreateOrganizationAsync();
        var adminId = await Seed.CreateUserAsync();
        var admin = SystemAdmin(adminId);

        var owner = await admin.DeleteAsync($"/api/admin/users/{org.OwnerId}");
        var self = await admin.DeleteAsync($"/api/admin/users/{adminId}");

        owner.StatusCode.Should().Be(HttpStatusCode.Conflict);
        self.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Lock_WritesAnAuditEntry_AndAnAdminCannotLockThemselves()
    {
        var target = await Seed.CreateUserAsync();
        var adminId = await Seed.CreateUserAsync();
        var admin = SystemAdmin(adminId);

        var locked = await admin.PostAsJsonAsync($"/api/admin/users/{target}/lock",
            new { reason = "Abuse", duration = "01:00:00" });
        var self = await admin.PostAsJsonAsync($"/api/admin/users/{adminId}/lock",
            new { reason = "Oops", duration = "01:00:00" });

        locked.StatusCode.Should().Be(HttpStatusCode.NoContent, await locked.Content.ReadAsStringAsync());
        (await Seed.CountAuditLogsAsync(target, "AdminLock")).Should().Be(1);
        self.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.CountAuditLogsAsync(adminId, "AdminLock")).Should().Be(0);
    }
}
