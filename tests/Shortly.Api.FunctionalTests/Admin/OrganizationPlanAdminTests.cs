using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Admin;

[Collection(ApiCollection.Name)]
public class OrganizationPlanAdminTests(ApiFixture api) : ApiTestBase(api)
{
    private static object Plan(string plan) => new { subscriptionPlanId = plan };

    private HttpClient SystemAdmin() => As(Guid.NewGuid(), UserRole.SystemAdmin);

    [Fact]
    public async Task NewOrganizations_AreCreatedOnTheFreePlan()
    {
        var owner = await Seed.CreateUserAsync();
        await Seed.SetUserPlanAsync(owner, enSubscriptionPlan.Enterprise); // the owner's personal plan must not matter

        var created = await As(owner).PostAsJsonAsync("/api/organizations", new { name = $"Org {Guid.NewGuid():N}" });
        created.StatusCode.Should().Be(HttpStatusCode.OK, await created.Content.ReadAsStringAsync());

        var mine = await ReadJsonAsync(await As(owner).GetAsync("/api/organizations/mine"));
        var orgId = mine.EnumerateArray().Single().GetProperty("organizationId").GetGuid();
        var (plan, isSubscribed) = await Seed.GetOrganizationPlanAsync(orgId);
        plan.Should().Be(enSubscriptionPlan.Free);
        isSubscribed.Should().BeFalse();
    }

    [Theory]
    [InlineData("Starter", enSubscriptionPlan.Starter, true)]
    [InlineData("Professional", enSubscriptionPlan.Professional, true)]
    [InlineData("Enterprise", enSubscriptionPlan.Enterprise, true)]
    [InlineData("Free", enSubscriptionPlan.Free, false)]
    public async Task SystemAdmin_CanSetTheOrganizationPlan(string requested, enSubscriptionPlan expected, bool subscribed)
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, requested == "Free" ? enSubscriptionPlan.Professional : enSubscriptionPlan.Free);

        var response = await SystemAdmin().PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", Plan(requested));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        (await Seed.GetOrganizationPlanAsync(org.OrganizationId)).Should().Be((expected, subscribed));
    }

    [Fact]
    public async Task SystemAdmin_CanSetThePlanByNumericValueToo_AndRepeatingItIsIdempotent()
    {
        var org = await CreateOrganizationAsync();
        var admin = SystemAdmin();

        (await admin.PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", new { subscriptionPlanId = 3 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", new { subscriptionPlanId = 3 }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await Seed.GetOrganizationPlanAsync(org.OrganizationId)).Plan.Should().Be(enSubscriptionPlan.Professional);
    }

    [Fact]
    public async Task ChangingOneOrganization_DoesNotChangeOtherOrganizationsOrThePersonalPlanOfItsMembers()
    {
        var target = await CreateOrganizationAsync();
        var other = await CreateOrganizationAsync();
        var ownerPlanBefore = await Seed.GetUserPlanAsync(target.OwnerId);

        await SystemAdmin().PatchAsJsonAsync($"/api/admin/organizations/{target.OrganizationId}/plan", Plan("Enterprise"));

        (await Seed.GetOrganizationPlanAsync(other.OrganizationId)).Plan.Should().Be(enSubscriptionPlan.Free);
        (await Seed.GetUserPlanAsync(target.OwnerId)).Should().Be(ownerPlanBefore);
    }

    // ---- authorization -----------------------------------------------------------------------------------

    [Fact]
    public async Task SuperAdmin_IsNotEnough()
    {
        var org = await CreateOrganizationAsync();

        var response = await As(Guid.NewGuid(), UserRole.SuperAdmin)
            .PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", Plan("Enterprise"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetOrganizationPlanAsync(org.OrganizationId)).Plan.Should().Be(enSubscriptionPlan.Free);
    }

    [Fact]
    public async Task OrganizationOwnerAndAdmin_CannotChangeTheirOwnPlan()
    {
        var org = await CreateOrganizationAsync();
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);

        var owner = As(org.OwnerId, organizationId: org.OrganizationId);
        var admin = org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin);

        (await owner.PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", Plan("Enterprise")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", Plan("Enterprise")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetOrganizationPlanAsync(org.OrganizationId)).Plan.Should().Be(enSubscriptionPlan.Free);
    }

    [Fact]
    public async Task RegularUser_Gets403_AndAnonymousGets401()
    {
        var org = await CreateOrganizationAsync();
        var user = await Seed.CreateUserAsync();

        (await As(user).PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", Plan("Enterprise")))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Anonymous().PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", Plan("Enterprise")))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- validation and missing resources ------------------------------------------------------------------

    [Fact]
    public async Task UnknownOrSoftDeletedOrganization_Returns404()
    {
        var deleted = await CreateOrganizationAsync();
        await Seed.SoftDeleteOrganizationAsync(deleted.OrganizationId);
        var admin = SystemAdmin();

        (await admin.PatchAsJsonAsync($"/api/admin/organizations/{Guid.NewGuid()}/plan", Plan("Professional")))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PatchAsJsonAsync($"/api/admin/organizations/{deleted.OrganizationId}/plan", Plan("Professional")))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("99")]
    [InlineData("0")]
    [InlineData("\"NotAPlan\"")]
    public async Task InvalidPlanValue_Returns400AndChangesNothing(string rawValue)
    {
        var org = await CreateOrganizationAsync();
        var content = new StringContent($"{{\"subscriptionPlanId\":{rawValue}}}", System.Text.Encoding.UTF8, "application/json");

        var response = await SystemAdmin().PatchAsync($"/api/admin/organizations/{org.OrganizationId}/plan", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Seed.GetOrganizationPlanAsync(org.OrganizationId)).Plan.Should().Be(enSubscriptionPlan.Free);
    }

    [Fact]
    public async Task OrganizationDetailEndpoint_ReportsTheNewPlan()
    {
        var org = await CreateOrganizationAsync();
        var admin = SystemAdmin();
        await admin.PatchAsJsonAsync($"/api/admin/organizations/{org.OrganizationId}/plan", Plan("Professional"));

        var detail = await ReadJsonAsync(await admin.GetAsync($"/api/admin/organizations/{org.OrganizationId}"));

        detail.GetProperty("subscriptionPlan").ToString().Should().BeOneOf("Professional", "3");
    }
}