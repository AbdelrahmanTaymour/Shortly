using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// An inactive (deactivated) or soft-deleted organization grants no access through otherwise active memberships,
/// not even to its owner. The owner and system administrators can still activate a deactivated organization.
/// </summary>
[Collection(ApiCollection.Name)]
public class InactiveOrganizationTests(ApiFixture api) : ApiTestBase(api)
{
    public enum OrgState { Deactivated, Deleted }

    public static IEnumerable<object[]> States => [[OrgState.Deactivated], [OrgState.Deleted]];

    private sealed record Setup(OrganizationFixture Org, HttpClient Owner, HttpClient Admin, HttpClient Member, long OrgLink);

    private async Task<Setup> ActiveOrganizationAsync()
    {
        var org = await CreateOrganizationAsync();
        var ownerMemberId = await Seed.GetMemberIdAsync(org.OrganizationId, org.OwnerId);
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var (memberId, memberMemberId) = await org.AddMemberAsync(MemberRole.Member);
        return new Setup(
            org,
            org.AsMember(org.OwnerId, ownerMemberId, MemberRole.OrgOwner),
            org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin),
            org.AsMember(memberId, memberMemberId, MemberRole.Member),
            await Seed.CreateOrganizationLinkAsync(org.OrganizationId, ownerMemberId));
    }

    private Task ApplyAsync(OrgState state, Guid organizationId) => state == OrgState.Deactivated
        ? Seed.DeactivateOrganizationAsync(organizationId)
        : Seed.SoftDeleteOrganizationAsync(organizationId);

    [Theory]
    [MemberData(nameof(States))]
    public async Task OrganizationResources_AreForbiddenToOwnerAdminAndMember(OrgState state)
    {
        var s = await ActiveOrganizationAsync();
        var orgId = s.Org.OrganizationId;
        await ApplyAsync(state, orgId);

        foreach (var (name, client) in new[] { ("owner", s.Owner), ("admin", s.Admin), ("member", s.Member) })
        {
            foreach (var url in new[]
                     {
                         $"/api/organizations/{orgId}/members",
                         $"/api/organizations/{orgId}/members/{s.Org.OwnerId}",
                         $"/api/organizations/{orgId}/teams",
                         $"/api/organizations/{orgId}/invitations",
                         $"/api/organizations/{orgId}/usage",
                         $"/api/short-urls/{s.OrgLink}",
                         "/api/short-urls/mine",
                         "/api/analytics/mine/summary"
                     })
            {
                var response = await client.GetAsync(url);
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{name} GET {url}");
            }

            (await client.GetAsync($"/api/analytics/urls/{s.OrgLink}/overview")).StatusCode
                .Should().Be(HttpStatusCode.NotFound, $"{name} per-URL analytics");
        }
    }

    [Theory]
    [MemberData(nameof(States))]
    public async Task OrganizationWrites_AreForbidden(OrgState state)
    {
        var s = await ActiveOrganizationAsync();
        var orgId = s.Org.OrganizationId;
        var newUser = await Seed.CreateUserAsync();
        await ApplyAsync(state, orgId);

        // A soft-deleted organization no longer exists for lookups (404); a deactivated one denies access (403).
        (await s.Admin.PostAsJsonAsync($"/api/organizations/{orgId}/members", new { userId = newUser, role = "Member" }))
            .StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
        (await s.Admin.PostAsJsonAsync($"/api/organizations/{orgId}/invitations",
                new { email = "someone@example.com", role = "Member", customPermissions = 0 }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.Owner.PostAsJsonAsync("/api/short-urls", new { originalUrl = "https://example.com/inactive-org" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.Member.DeleteAsync($"/api/short-urls/{s.OrgLink}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await Seed.GetMemberStateAsync(orgId, newUser)).Should().BeNull();
        (await Seed.LinkExistsAsync(s.OrgLink)).Should().BeTrue();
        (await Seed.CountOrganizationLinksAsync(orgId)).Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(States))]
    public async Task SwitchingIntoTheOrganization_IsDenied(OrgState state)
    {
        var org = await CreateOrganizationAsync();
        await ApplyAsync(state, org.OrganizationId);

        var response = await As(org.OwnerId).PostAsync($"/api/organizations/{org.OrganizationId}/switch", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SwitchingIntoAnActiveOrganization_StillWorks()
    {
        var org = await CreateOrganizationAsync();

        (await As(org.OwnerId).PostAsync($"/api/organizations/{org.OrganizationId}/switch", null)).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    // ---- activation keeps working ------------------------------------------------------------------------------

    [Fact]
    public async Task Owner_CanActivateADeactivatedOrganization_AndAccessIsRestored()
    {
        var s = await ActiveOrganizationAsync();
        await Seed.DeactivateOrganizationAsync(s.Org.OrganizationId);
        (await s.Member.GetAsync($"/api/organizations/{s.Org.OrganizationId}/members")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);

        (await s.Owner.PostAsync($"/api/organizations/{s.Org.OrganizationId}/activate", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await s.Member.GetAsync($"/api/organizations/{s.Org.OrganizationId}/members")).StatusCode
            .Should().Be(HttpStatusCode.OK);
        (await s.Member.GetAsync("/api/short-urls/mine")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemAdmin_CanActivateADeactivatedOrganization()
    {
        var s = await ActiveOrganizationAsync();
        await Seed.DeactivateOrganizationAsync(s.Org.OrganizationId);

        var response = await As(Guid.NewGuid(), UserRole.SystemAdmin)
            .PostAsync($"/api/organizations/{s.Org.OrganizationId}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.OrganizationIsActiveAsync(s.Org.OrganizationId)).Should().BeTrue();
    }

    [Fact]
    public async Task AdminWhoIsNotTheOwner_CannotActivateADeactivatedOrganization()
    {
        var s = await ActiveOrganizationAsync();
        await Seed.DeactivateOrganizationAsync(s.Org.OrganizationId);

        (await s.Admin.PostAsync($"/api/organizations/{s.Org.OrganizationId}/activate", null)).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- listing ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task MyOrganizations_StillListsADeactivatedOrganization_SoItCanBeReactivated_ButNotADeletedOne()
    {
        var deactivated = await CreateOrganizationAsync();
        var deleted = await CreateOrganizationAsync();
        var owner = await Seed.CreateUserAsync();
        await Seed.AddMemberAsync(deactivated.OrganizationId, owner, MemberRole.Member);
        await Seed.AddMemberAsync(deleted.OrganizationId, owner, MemberRole.Member);
        await Seed.DeactivateOrganizationAsync(deactivated.OrganizationId);
        await Seed.SoftDeleteOrganizationAsync(deleted.OrganizationId);

        var json = await ReadJsonAsync(await As(owner).GetAsync("/api/organizations/mine"));

        var ids = json.EnumerateArray().Select(o => o.GetProperty("organizationId").GetGuid()).ToList();
        ids.Should().Contain(deactivated.OrganizationId);
        ids.Should().NotContain(deleted.OrganizationId);
    }

    // ---- refresh ---------------------------------------------------------------------------------------------------

    private async Task<(string RefreshToken, Guid OrgId)> SwitchedRefreshTokenAsync()
    {
        var org = await CreateOrganizationAsync();
        var switched = await As(org.OwnerId).PostAsync($"/api/organizations/{org.OrganizationId}/switch", null);
        switched.StatusCode.Should().Be(HttpStatusCode.OK);
        return ((await ReadJsonAsync(switched)).GetProperty("refreshToken").GetString()!, org.OrganizationId);
    }

    private async Task<string?> OrganizationClaimAfterRefreshAsync(string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token");
        request.Headers.Add("Cookie", $"refreshToken={Uri.EscapeDataString(refreshToken)}");
        var response = await Api.Default.CreateApiClient().SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var accessToken = (await ReadJsonAsync(response)).GetProperty("accessToken").GetString()!;
        return new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims
            .FirstOrDefault(c => c.Type == "organization_id")?.Value;
    }

    [Fact]
    public async Task Refresh_ForAnActiveOrganization_KeepsTheOrganizationClaim()
    {
        var (refreshToken, orgId) = await SwitchedRefreshTokenAsync();

        (await OrganizationClaimAfterRefreshAsync(refreshToken)).Should().Be(orgId.ToString());
    }

    [Theory]
    [MemberData(nameof(States))]
    public async Task Refresh_ForAnInactiveOrDeletedOrganization_DoesNotIssueAnOrganizationClaim(OrgState state)
    {
        var (refreshToken, orgId) = await SwitchedRefreshTokenAsync();
        await ApplyAsync(state, orgId);

        (await OrganizationClaimAfterRefreshAsync(refreshToken)).Should().BeNull();
    }

    [Fact]
    public async Task Refresh_AfterTheMemberWasRemoved_DoesNotIssueAnOrganizationClaim()
    {
        var org = await CreateOrganizationAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member);
        var switched = await As(userId).PostAsync($"/api/organizations/{org.OrganizationId}/switch", null);
        var refreshToken = (await ReadJsonAsync(switched)).GetProperty("refreshToken").GetString()!;
        await Seed.RemoveMemberAsync(org.OrganizationId, userId);

        (await OrganizationClaimAfterRefreshAsync(refreshToken)).Should().BeNull();
    }

    // ---- owner-level management is unchanged -------------------------------------------------------------------------

    [Fact]
    public async Task Owner_CanStillDeleteADeactivatedOrganization()
    {
        var s = await ActiveOrganizationAsync();
        await Seed.DeactivateOrganizationAsync(s.Org.OrganizationId);

        (await s.Owner.DeleteAsync($"/api/organizations/{s.Org.OrganizationId}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
    }
}
