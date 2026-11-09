using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.SubscriptionPlan;

namespace Shortly.Api.FunctionalTests.Organizations;

[Collection(ApiCollection.Name)]
public class OrganizationScopedRouteTests(ApiFixture api) : ApiTestBase(api)
{
    [Fact]
    public async Task Member_WithAPersonalToken_ListsOrganizationLinksThroughTheRoute()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member);
        var linkId = await Seed.CreateOrganizationLinkAsync(org.OrganizationId, memberId);

        var personal = await As(userId).GetAsync("/api/short-urls/mine");
        var scoped = await As(userId).GetAsync($"/api/organizations/{org.OrganizationId}/short-urls/mine");

        personal.StatusCode.Should().Be(HttpStatusCode.OK);
        (await personal.Content.ReadAsStringAsync()).Should().NotContain($"\"id\":{linkId}");
        scoped.StatusCode.Should().Be(HttpStatusCode.OK);
        (await scoped.Content.ReadAsStringAsync()).Should().Contain($"\"id\":{linkId}");
    }

    [Fact]
    public async Task NonMember_GetsForbidden_AndAnonymousGetsUnauthorized()
    {
        var org = await CreateOrganizationAsync();
        var url = $"/api/organizations/{org.OrganizationId}/short-urls/mine";

        (await As(Guid.NewGuid()).GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Anonymous().GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RouteOrganization_WinsOverTheTokenOrganizationClaim()
    {
        var first = await CreateOrganizationAsync();
        var second = await CreateOrganizationAsync();
        var (userId, memberId) = await second.AddMemberAsync(MemberRole.Member);
        var secondLink = await Seed.CreateOrganizationLinkAsync(second.OrganizationId, memberId);

        var response = await As(userId, organizationId: first.OrganizationId)
            .GetAsync($"/api/organizations/{second.OrganizationId}/short-urls/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain($"\"id\":{secondLink}");
    }

    [Fact]
    public async Task Viewer_CannotCreateThroughTheRoute()
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, enSubscriptionPlan.Professional);
        var (userId, _) = await org.AddMemberAsync(MemberRole.Viewer);

        var response = await As(userId).PostAsJsonAsync($"/api/organizations/{org.OrganizationId}/short-urls",
            new { originalUrl = "https://example.com/scoped-viewer" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Member_CreatesAnOrganizationLinkThroughTheRoute()
    {
        var org = await CreateOrganizationAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member);

        var response = await As(userId).PostAsJsonAsync($"/api/organizations/{org.OrganizationId}/short-urls",
            new { originalUrl = "https://example.com/scoped-member" });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        (await Seed.CountOrganizationLinksAsync(org.OrganizationId)).Should().Be(1);
    }

    [Fact]
    public async Task Analytics_AreReachableUnderTheOrganizationRoute()
    {
        var org = await CreateOrganizationAsync();
        await Seed.SetOrganizationPlanAsync(org.OrganizationId, enSubscriptionPlan.Professional);
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member);

        var response = await As(userId).GetAsync($"/api/organizations/{org.OrganizationId}/analytics/mine/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Search_WithInvalidPaging_Returns400()
    {
        var user = await Seed.CreateUserAsync();

        (await As(user).GetAsync("/api/short-urls/search?page=0&pageSize=10")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await As(user).GetAsync("/api/short-urls/search?page=1&pageSize=100000")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Swagger_GeneratesWithTheDuplicatedRoutes()
    {
        var response = await Anonymous().GetAsync("/swagger/v1/swagger.json");

        // Swagger may be disabled outside Development; only a generation failure (500) is a regression.
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
