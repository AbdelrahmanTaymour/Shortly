using System.Net;
using System.Net.Http.Json;
using Shortly.Api.FunctionalTests.Infrastructure;
using Shortly.Domain.Members;

namespace Shortly.Api.FunctionalTests;

/// <summary>
/// Re-adding / re-inviting a previously removed member reactivates their existing membership, and inactive members
/// can never be the target of team or role operations.
/// </summary>
[Collection(ApiCollection.Name)]
public class MembershipConsistencyTests(ApiFixture api) : ApiTestBase(api)
{
    private async Task<(OrganizationFixture Org, HttpClient Admin)> OrgWithAdminAsync()
    {
        var org = await CreateOrganizationAsync();
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        return (org, org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin));
    }

    // ---- AddMember: reactivation ---------------------------------------------------------------------

    [Fact]
    public async Task AddMember_ForRemovedMember_ReactivatesTheExistingRowWithTheRequestedRole()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.OrgAdmin, isActive: false);
        var before = await Seed.GetMemberStateAsync(org.OrganizationId, userId);

        var response = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/members", new { userId, role = "Viewer" });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var after = await Seed.GetMemberStateAsync(org.OrganizationId, userId);
        after!.IsActive.Should().BeTrue();
        after.Role.Should().Be(MemberRole.Viewer);
        after.Id.Should().Be(memberId);                 // the same row, not a new one
        after.JoinedAt.Should().BeAfter(before!.JoinedAt);   // a returning member gets a fresh join date; InvitedBy is preserved
        after.InvitedBy.Should().Be(before.InvitedBy);
        (await Seed.CountMembershipRowsAsync(org.OrganizationId, userId)).Should().Be(1);
    }

    [Fact]
    public async Task AddMember_ForActiveMember_Returns409()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member);

        var response = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/members", new { userId, role = "Viewer" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.Role.Should().Be(MemberRole.Member);
    }

    [Fact]
    public async Task AddMember_ForBrandNewUser_StillCreatesAMembership()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var userId = await Seed.CreateUserAsync();

        var response = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/members", new { userId, role = "Member" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddMember_ReactivationStillHonorsTheMemberLimit()
    {
        var org = await CreateOrganizationAsync(); // limit is 25; the owner is member #1
        var (adminId, adminMemberId) = await org.AddMemberAsync(MemberRole.OrgAdmin);
        var admin = org.AsMember(adminId, adminMemberId, MemberRole.OrgAdmin);
        var (removedUser, _) = await org.AddMemberAsync(MemberRole.Member, isActive: false);
        for (var i = 0; i < 23; i++) await org.AddMemberAsync(MemberRole.Member); // 25 active members now

        var response = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/members", new { userId = removedUser, role = "Member" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetMemberStateAsync(org.OrganizationId, removedUser))!.IsActive.Should().BeFalse();
    }

    // ---- AcceptInvitation ------------------------------------------------------------------------------

    [Fact]
    public async Task AcceptInvitation_ByRemovedMember_ReactivatesTheExistingRow()
    {
        var org = await CreateOrganizationAsync();
        var (userId, memberId) = await org.AddMemberAsync(MemberRole.OrgAdmin, isActive: false);
        var before = await Seed.GetMemberStateAsync(org.OrganizationId, userId);
        var email = await Seed.GetUserEmailAsync(userId);
        var token = await Seed.CreateInvitationAsync(org.OrganizationId, email, MemberRole.Member, org.OwnerId);

        var response = await As(userId).PostAsync($"/api/invitations/accept?token={Uri.EscapeDataString(token)}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var after = await Seed.GetMemberStateAsync(org.OrganizationId, userId);
        after!.IsActive.Should().BeTrue();
        after.Role.Should().Be(MemberRole.Member);       // the invited role, not the old one
        after.Id.Should().Be(memberId);
        after.JoinedAt.Should().BeAfter(before!.JoinedAt);
        after.InvitedBy.Should().Be(before.InvitedBy);
        (await Seed.CountMembershipRowsAsync(org.OrganizationId, userId)).Should().Be(1);
    }

    [Fact]
    public async Task AcceptInvitation_ByNewUser_CreatesMembership()
    {
        var org = await CreateOrganizationAsync();
        var userId = await Seed.CreateUserAsync();
        var token = await Seed.CreateInvitationAsync(
            org.OrganizationId, await Seed.GetUserEmailAsync(userId), MemberRole.Viewer, org.OwnerId);

        var response = await As(userId).PostAsync($"/api/invitations/accept?token={Uri.EscapeDataString(token)}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.Role.Should().Be(MemberRole.Viewer);
    }

    [Fact]
    public async Task AcceptInvitation_ByAlreadyActiveMember_Returns409AndKeepsInvitationPending()
    {
        var org = await CreateOrganizationAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member);
        var email = await Seed.GetUserEmailAsync(userId);
        var token = await Seed.CreateInvitationAsync(org.OrganizationId, email, MemberRole.OrgAdmin, org.OwnerId);

        var response = await As(userId).PostAsync($"/api/invitations/accept?token={Uri.EscapeDataString(token)}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.Role.Should().Be(MemberRole.Member);
        (await Seed.InvitationIsPendingAsync(org.OrganizationId, email)).Should().BeTrue();
    }

    [Fact]
    public async Task AcceptInvitation_WithAnotherUsersEmail_IsRejected()
    {
        var org = await CreateOrganizationAsync();
        var invitee = await Seed.CreateUserAsync();
        var other = await Seed.CreateUserAsync();
        var token = await Seed.CreateInvitationAsync(
            org.OrganizationId, await Seed.GetUserEmailAsync(invitee), MemberRole.Member, org.OwnerId);

        var response = await As(other).PostAsync($"/api/invitations/accept?token={Uri.EscapeDataString(token)}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetMemberStateAsync(org.OrganizationId, other)).Should().BeNull();
    }

    [Fact]
    public async Task AcceptInvitation_WhenTheOrganizationIsFull_Returns403AndKeepsTheInvitationPending()
    {
        var owner = await Seed.CreateUserAsync();
        var orgId = await Seed.CreateOrganizationAsync(owner, memberLimit: 2);      // owner + one more
        await Seed.AddMemberAsync(orgId, await Seed.CreateUserAsync(), MemberRole.Member);   // now full
        var invitee = await Seed.CreateUserAsync();
        var email = await Seed.GetUserEmailAsync(invitee);
        var token = await Seed.CreateInvitationAsync(orgId, email, MemberRole.Member, owner);

        var response = await As(invitee).PostAsync($"/api/invitations/accept?token={Uri.EscapeDataString(token)}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetMemberStateAsync(orgId, invitee)).Should().BeNull();
        (await Seed.InvitationIsPendingAsync(orgId, email)).Should().BeTrue();
    }

    [Fact]
    public async Task AcceptInvitation_ByARemovedMember_AlsoRespectsTheLimit()
    {
        var owner = await Seed.CreateUserAsync();
        var orgId = await Seed.CreateOrganizationAsync(owner, memberLimit: 2);
        var removed = await Seed.CreateUserAsync();
        await Seed.AddMemberAsync(orgId, removed, MemberRole.Member, isActive: false);
        await Seed.AddMemberAsync(orgId, await Seed.CreateUserAsync(), MemberRole.Member);   // owner + one: full
        var token = await Seed.CreateInvitationAsync(orgId, await Seed.GetUserEmailAsync(removed), MemberRole.Member, owner);

        var response = await As(removed).PostAsync($"/api/invitations/accept?token={Uri.EscapeDataString(token)}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Seed.GetMemberStateAsync(orgId, removed))!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptInvitation_WithRoomLeft_StillSucceeds()
    {
        var owner = await Seed.CreateUserAsync();
        var orgId = await Seed.CreateOrganizationAsync(owner, memberLimit: 2);     // owner + one free seat
        var invitee = await Seed.CreateUserAsync();
        var token = await Seed.CreateInvitationAsync(orgId, await Seed.GetUserEmailAsync(invitee), MemberRole.Member, owner);

        (await As(invitee).PostAsync($"/api/invitations/accept?token={Uri.EscapeDataString(token)}", null)).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
    }

    // ---- UpdateMemberRole ----------------------------------------------------------------------------------

    [Fact]
    public async Task UpdateMemberRole_OnInactiveMember_Returns404AndChangesNothing()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member, isActive: false);

        var response = await admin.PatchAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/members/{userId}/role", new { role = "OrgAdmin" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.Role.Should().Be(MemberRole.Member);
    }

    [Fact]
    public async Task UpdateMemberRole_OnActiveMember_Succeeds()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member);

        var response = await admin.PatchAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/members/{userId}/role", new { role = "TeamManager" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Seed.GetMemberStateAsync(org.OrganizationId, userId))!.Role.Should().Be(MemberRole.TeamManager);
    }

    // ---- teams: inactive members cannot be assigned ----------------------------------------------------------

    [Fact]
    public async Task CreateTeam_WithInactiveManager_IsRejected()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, inactiveMemberId) = await org.AddMemberAsync(MemberRole.Member, isActive: false);

        var response = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/teams", new { teamManagerId = inactiveMemberId, name = $"T{Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTeam_WithActiveManager_Succeeds()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, memberId) = await org.AddMemberAsync(MemberRole.Member);

        var response = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/teams", new { teamManagerId = memberId, name = $"T{Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ChangeTeamManager_ToInactiveMember_IsRejectedAndManagerUnchanged()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
        var (_, inactiveMemberId) = await org.AddMemberAsync(MemberRole.Member, isActive: false);

        var response = await admin.PatchAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/teams/{teamId}/manager", new { newManagerId = inactiveMemberId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Seed.GetTeamManagerAsync(teamId)).Should().Be(managerMemberId);
    }

    [Fact]
    public async Task AddTeamMember_InactiveMember_IsRejected_ActiveMemberSucceeds()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (_, managerMemberId) = await org.AddMemberAsync(MemberRole.TeamManager);
        var teamId = await Seed.CreateTeamAsync(org.OrganizationId, managerMemberId);
        var (_, inactiveMemberId) = await org.AddMemberAsync(MemberRole.Member, isActive: false);
        var (_, activeMemberId) = await org.AddMemberAsync(MemberRole.Member);

        var rejected = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/teams/{teamId}/members", new { memberId = inactiveMemberId });
        var accepted = await admin.PostAsJsonAsync(
            $"/api/organizations/{org.OrganizationId}/teams/{teamId}/members", new { memberId = activeMemberId });

        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Seed.IsTeamMemberAsync(teamId, inactiveMemberId)).Should().BeFalse();
        accepted.IsSuccessStatusCode.Should().BeTrue(await accepted.Content.ReadAsStringAsync());
        (await Seed.IsTeamMemberAsync(teamId, activeMemberId)).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveMember_OnAlreadyInactiveMember_StillReportsConflict()
    {
        var (org, admin) = await OrgWithAdminAsync();
        var (userId, _) = await org.AddMemberAsync(MemberRole.Member, isActive: false);

        var response = await admin.DeleteAsync($"/api/organizations/{org.OrganizationId}/members/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
