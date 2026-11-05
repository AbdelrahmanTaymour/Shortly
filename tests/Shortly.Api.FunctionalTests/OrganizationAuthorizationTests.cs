using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests;

/// <summary>Phase 0.1 / 0.2 / 0.3: organization resources are only reachable by authorized callers.</summary>
[Collection(ApiCollection.Name)]
public class OrganizationAuthorizationTests(ApiFixture api) : ApiTestBase(api)
{
    // ---- Members list -------------------------------------------------------------------------

    [Fact]
    public async Task GetMembers_ByUserWhoIsNotAMember_Returns403()
    {
        var org = await CreateOrganizationAsync();
        var outsider = await Seed.CreateUserAsync();

        var response = await As(outsider).GetAsync($"/api/organizations/{org.OrganizationId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMembers_ByMemberOfAnotherOrganization_Returns403()
    {
        var target = await CreateOrganizationAsync();
        var other = await CreateOrganizationAsync();

        // The other organization's owner is a legitimate member of THEIR organization, not of the target.
        var response = await As(other.OwnerId, organizationId: other.OrganizationId)
            .GetAsync($"/api/organizations/{target.OrganizationId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.OrgAdmin)]
    public async Task GetMembers_ByActiveMember_Returns200WithMembers(MemberRole role)
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(role);

        var response = await org.AsMember(userId, memberId, role).GetAsync($"/api/organizations/{org.OrganizationId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.GetProperty("totalCount").GetInt32().Should().Be(2); // owner + this member
    }

    [Fact]
    public async Task GetMembers_ByRemovedMember_Returns403()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Member, isActive: false);

        var response = await org.AsMember(userId, memberId, MemberRole.Member)
            .GetAsync($"/api/organizations/{org.OrganizationId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMembers_Anonymous_Returns401()
    {
        var org = await CreateOrganizationAsync();

        var response = await Anonymous().GetAsync($"/api/organizations/{org.OrganizationId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Single membership --------------------------------------------------------------------

    [Fact]
    public async Task GetMembership_ByNonMember_Returns403()
    {
        var org = await CreateOrganizationAsync();
        var outsider = await Seed.CreateUserAsync();

        var response = await As(outsider).GetAsync($"/api/organizations/{org.OrganizationId}/members/{org.OwnerId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMembership_ByActiveMember_CanViewAnotherMembersMembership()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.Viewer);

        var response = await org.AsMember(userId, memberId, MemberRole.Viewer)
            .GetAsync($"/api/organizations/{org.OrganizationId}/members/{org.OwnerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("userId").GetGuid().Should().Be(org.OwnerId);
    }

    // ---- Usage --------------------------------------------------------------------------------

    [Fact]
    public async Task GetUsage_ByNonMember_Returns403()
    {
        var org = await CreateOrganizationAsync();
        var outsider = await Seed.CreateUserAsync();

        var response = await As(outsider).GetAsync($"/api/organizations/{org.OrganizationId}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task GetUsage_ByRegularMember_Returns403(MemberRole role)
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(role);

        var response = await org.AsMember(userId, memberId, role).GetAsync($"/api/organizations/{org.OrganizationId}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsage_ByAdmin_Returns200()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);

        var response = await org.AsMember(userId, memberId, MemberRole.OrgAdmin)
            .GetAsync($"/api/organizations/{org.OrganizationId}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetUsage_ByOwner_Returns200()
    {
        var org = await CreateOrganizationAsync();

        var response = await As(org.OwnerId, organizationId: org.OrganizationId)
            .GetAsync($"/api/organizations/{org.OrganizationId}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsage_BySystemAdminWhoIsNotAMember_Returns200()
    {
        var org = await CreateOrganizationAsync();

        var response = await As(Guid.NewGuid(), UserRole.SystemAdmin).GetAsync($"/api/organizations/{org.OrganizationId}/usage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsage_ByRemovedAdminWithStillValidToken_Returns403()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var token = org.AsMember(userId, memberId, MemberRole.OrgAdmin); // token issued while still admin
        (await token.GetAsync($"/api/organizations/{org.OrganizationId}/usage")).StatusCode.Should().Be(HttpStatusCode.OK);

        await Seed.RemoveMemberAsync(org.OrganizationId, userId);

        var response = await token.GetAsync($"/api/organizations/{org.OrganizationId}/usage");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Activation ---------------------------------------------------------------------------

    [Fact]
    public async Task Deactivate_ByUnrelatedUser_Returns403AndOrganizationStaysActive()
    {
        var org = await CreateOrganizationAsync();
        var outsider = await Seed.CreateUserAsync();

        var response = await As(outsider).PostAsync($"/api/organizations/{org.OrganizationId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.OrganizationIsActiveAsync(org.OrganizationId)).Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_ByOrgAdminWhoIsNotTheOwner_Returns403()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);

        var response = await org.AsMember(userId, memberId, MemberRole.OrgAdmin)
            .PostAsync($"/api/organizations/{org.OrganizationId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.OrganizationIsActiveAsync(org.OrganizationId)).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateThenActivate_ByOwner_Succeeds()
    {
        var org = await CreateOrganizationAsync();
        var owner = As(org.OwnerId, organizationId: org.OrganizationId);

        (await owner.PostAsync($"/api/organizations/{org.OrganizationId}/deactivate", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await Seed.OrganizationIsActiveAsync(org.OrganizationId)).Should().BeFalse();

        (await owner.PostAsync($"/api/organizations/{org.OrganizationId}/activate", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await Seed.OrganizationIsActiveAsync(org.OrganizationId)).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateThenActivate_BySystemAdminWhoIsNotTheOwner_Succeeds()
    {
        var org = await CreateOrganizationAsync();
        var admin = As(Guid.NewGuid(), UserRole.SystemAdmin);

        (await admin.PostAsync($"/api/organizations/{org.OrganizationId}/deactivate", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await Seed.OrganizationIsActiveAsync(org.OrganizationId)).Should().BeFalse();

        (await admin.PostAsync($"/api/organizations/{org.OrganizationId}/activate", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
    }

    // ---- Removed members lose their old role (inactive-membership bug) -------------------------

    [Fact]
    public async Task AddMember_ByRemovedAdminWithStillValidToken_Returns403()
    {
        var org = await CreateOrganizationAsync();
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var newUser = await Seed.CreateUserAsync();
        var admin = org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin);
        await Seed.RemoveMemberAsync(org.OrganizationId, adminId);

        var response = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/members", new { userId = newUser, role = "Member" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListInvitations_ByRemovedAdmin_Returns403_ButActiveAdminSucceeds()
    {
        var org = await CreateOrganizationAsync();
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var admin = org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin);

        (await admin.GetAsync($"/api/organizations/{org.OrganizationId}/invitations")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        await Seed.RemoveMemberAsync(org.OrganizationId, adminId);

        (await admin.GetAsync($"/api/organizations/{org.OrganizationId}/invitations")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }
}
