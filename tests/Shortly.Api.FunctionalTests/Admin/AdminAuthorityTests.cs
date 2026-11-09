using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Api.FunctionalTests.Admin;

[Collection(ApiCollection.Name)]
public class AdminAuthorityTests(ApiFixture api) : ApiTestBase(api)
{
    private sealed record Setup(OrganizationFixture Org, HttpClient Admin, Guid PeerAdminId, Guid MemberUserId);

    private async Task<Setup> OrganizationAsync()
    {
        var org = await CreateOrganizationAsync();
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var (peerId, _) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var (memberUserId, _) = await org.AddMemberAsync(MemberRole.Member);
        return new Setup(org, org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin), peerId, memberUserId);
    }

    private static string Members(Guid organizationId, Guid userId) => $"/api/organizations/{organizationId}/members/{userId}";

    // ---- what an admin CAN do -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Admin_CanDemoteAndRemoveAPeerAdmin()
    {
        var s = await OrganizationAsync();

        (await s.Admin.PatchAsJsonAsync($"{Members(s.Org.OrganizationId, s.PeerAdminId)}/role", new { role = "Member" })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.PeerAdminId))!.Role.Should().Be(MemberRole.Member);

        var (anotherAdmin, _) = await s.Org.AddMemberAsync(MemberRole.OrgAdmin);
        (await s.Admin.DeleteAsync(Members(s.Org.OrganizationId, anotherAdmin))).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, anotherAdmin))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_CanPromoteAMemberToAdmin_AndRemoveMembers()
    {
        var s = await OrganizationAsync();

        (await s.Admin.PatchAsJsonAsync($"{Members(s.Org.OrganizationId, s.MemberUserId)}/role", new { role = "OrgAdmin" })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.MemberUserId))!.Role.Should().Be(MemberRole.OrgAdmin);

        (await s.Admin.DeleteAsync(Members(s.Org.OrganizationId, s.MemberUserId))).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---- what an admin can NOT do -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Admin_CannotRemoveOrModifyTheOwner_OrGrantOwnership()
    {
        var s = await OrganizationAsync();

        (await s.Admin.DeleteAsync(Members(s.Org.OrganizationId, s.Org.OwnerId))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await s.Admin.PatchAsJsonAsync($"{Members(s.Org.OrganizationId, s.Org.OwnerId)}/role", new { role = "Member" })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
        (await s.Admin.PatchAsJsonAsync($"{Members(s.Org.OrganizationId, s.MemberUserId)}/role", new { role = "OrgOwner" })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);

        var owner = (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.Org.OwnerId))!;
        owner.IsActive.Should().BeTrue();
        owner.Role.Should().Be(MemberRole.OrgOwner);
        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.MemberUserId))!.Role.Should().Be(MemberRole.Member);
    }

    [Fact]
    public async Task Admin_CannotAddOrInviteAnOwner()
    {
        var s = await OrganizationAsync();
        var newUser = await Seed.CreateUserAsync();

        (await s.Admin.PostAsJsonAsync($"/api/organizations/{s.Org.OrganizationId}/members", new { userId = newUser, role = "OrgOwner" })).StatusCode
            .Should().Be(HttpStatusCode.BadRequest);
        (await s.Admin.PostAsJsonAsync($"/api/organizations/{s.Org.OrganizationId}/invitations",
            new { email = "owner@example.com", role = "OrgOwner", customPermissions = 0 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_CannotTransferOwnership_DeleteUpdateOrDeactivateTheOrganization()
    {
        var s = await OrganizationAsync();
        var orgId = s.Org.OrganizationId;

        (await s.Admin.PostAsJsonAsync($"/api/organizations/{orgId}/transfer-ownership", new { newOwnerId = s.PeerAdminId })).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await s.Admin.DeleteAsync($"/api/organizations/{orgId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.Admin.PutAsJsonAsync($"/api/organizations/{orgId}", new
        {
            name = "Renamed by admin", description = "d", website = (string?)null, logoUrl = (string?)null, memberLimit = 10
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.Admin.PostAsync($"/api/organizations/{orgId}/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await Seed.OrganizationIsActiveAsync(orgId)).Should().BeTrue();
        (await Seed.GetMemberStateAsync(orgId, s.Org.OwnerId))!.Role.Should().Be(MemberRole.OrgOwner);
    }

    // ---- roles below admin have no member-management authority ------------------------------------------------------------

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public async Task RolesBelowAdmin_CannotRemoveOrChangeAnyone(MemberRole role)
    {
        var s = await OrganizationAsync();
        var (userId, memberId) = await s.Org.AddMemberAsync(role);
        var client = s.Org.AsMember(userId, memberId, role);

        (await client.DeleteAsync(Members(s.Org.OrganizationId, s.MemberUserId))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PatchAsJsonAsync($"{Members(s.Org.OrganizationId, s.PeerAdminId)}/role", new { role = "Viewer" })).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);

        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.PeerAdminId))!.Role.Should().Be(MemberRole.OrgAdmin);
        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.MemberUserId))!.IsActive.Should().BeTrue();
    }

    // ---- the owner keeps the exclusive powers -------------------------------------------------------------------------------

    [Fact]
    public async Task Owner_CanTransferOwnership_ToAnAdmin_AndTheOldOwnerBecomesAMember()
    {
        var s = await OrganizationAsync();
        var owner = As(s.Org.OwnerId, organizationId: s.Org.OrganizationId);

        (await owner.PostAsJsonAsync($"/api/organizations/{s.Org.OrganizationId}/transfer-ownership", new { newOwnerId = s.PeerAdminId })).StatusCode
            .Should().Be(HttpStatusCode.NoContent);

        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.PeerAdminId))!.Role.Should().Be(MemberRole.OrgOwner);
        (await Seed.GetMemberStateAsync(s.Org.OrganizationId, s.Org.OwnerId))!.Role.Should().Be(MemberRole.Member);
    }
}
