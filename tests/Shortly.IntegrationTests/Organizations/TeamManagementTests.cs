using Microsoft.EntityFrameworkCore;
using Shortly.Core.Teams.AddTeamMember;
using Shortly.Core.Teams.ChangeTeamManager;
using Shortly.Core.Teams.CheckTeamNameAvailability;
using Shortly.Core.Teams.CreateTeam;
using Shortly.Core.Teams.DeleteTeam;
using Shortly.Core.Teams.GetManagedTeams;
using Shortly.Core.Teams.GetOrganizationTeams;
using Shortly.Core.Teams.GetTeam;
using Shortly.Core.Teams.GetTeamMembers;
using Shortly.Core.Teams.RemoveTeamMember;
using Shortly.Core.Teams.UpdateTeam;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Organizations;

public class TeamManagementTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<OrganizationTeam> Stored(Guid teamId) => Query(db => db.Set<OrganizationTeam>().SingleAsync(t => t.Id == teamId));

    private Task<bool> IsOnTeam(Guid teamId, Guid memberId) =>
        Query(db => db.Set<OrganizationTeamMember>().AnyAsync(tm => tm.TeamId == teamId && tm.MemberId == memberId));

    private async Task<(TestData.OrgFixture Org, Shortly.Domain.Users.User ManagerUser, OrganizationMember Manager)> OrgWithManager()
    {
        var org = await Data.OrganizationAsync();
        var (user, row) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);
        return (org, user, row);
    }

    // ---- create ----------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_Should_StoreTheTeam_AndPutTheManagerOnIt()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        As(org.Owner.Id);

        // Act
        var result = await Send(new CreateTeamCommand(org.Id, manager.Id, "Marketing", "Campaigns"));

        // Assert
        var team = await Stored(result.Value);
        team.Name.Should().Be("Marketing");
        team.Description.Should().Be("Campaigns");
        team.TeamManagerId.Should().Be(manager.Id);
        (await IsOnTeam(team.Id, manager.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Create_Should_Refuse_ANonAdmin_ALowRoleManager_ARemovedMember_AndAForeignMember()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        var (viewer, viewerRow) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        var (_, removedRow) = await Data.MemberAsync(org.Id, MemberRole.TeamManager, isActive: false);
        var otherOrg = await Data.OrganizationAsync();

        // Act
        As(manager.UserId());
        var byManager = await Send(new CreateTeamCommand(org.Id, manager.Id, "A", null));
        As(org.Owner.Id);
        var lowRole = await Send(new CreateTeamCommand(org.Id, viewerRow.Id, "B", null));
        var removed = await Send(new CreateTeamCommand(org.Id, removedRow.Id, "C", null));
        var foreign = await Send(new CreateTeamCommand(org.Id, otherOrg.OwnerMember.Id, "D", null));

        // Assert
        byManager.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        lowRole.Error.Should().Be(OrganizationTeamErrors.ManagerRoleTooLow);
        removed.Error.Should().Be(OrganizationTeamErrors.ManagerNotOrgMember);
        foreign.Error.Should().Be(OrganizationTeamErrors.ManagerNotOrgMember);
        (await Query(db => db.Set<OrganizationTeam>().CountAsync(t => t.OrganizationId == org.Id))).Should().Be(0);
    }

    [Fact]
    public async Task Create_Should_ReturnNameTaken_ForADuplicateInTheOrganization_ButAllowItInAnother()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        var (otherOrg, _, otherManager) = await OrgWithManager();
        As(org.Owner.Id);
        await Send(new CreateTeamCommand(org.Id, manager.Id, "Marketing", null));

        // Act
        var duplicate = await Send(new CreateTeamCommand(org.Id, manager.Id, "Marketing", null));
        As(otherOrg.Owner.Id);
        var elsewhere = await Send(new CreateTeamCommand(otherOrg.Id, otherManager.Id, "Marketing", null));

        // Assert
        duplicate.Error.Should().Be(OrganizationTeamErrors.NameTaken);
        elsewhere.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_Should_ReturnNameTaken_ForTheSameNameInADifferentCase()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        As(org.Owner.Id);
        await Send(new CreateTeamCommand(org.Id, manager.Id, "Marketing", null));

        // Act
        var result = await Send(new CreateTeamCommand(org.Id, manager.Id, "MARKETING", null));
        var availability = await Send(new CheckTeamNameAvailabilityCommand(org.Id, "marketing"));

        // Assert: the documented rule is "unique ignoring case"
        result.Error.Should().Be(OrganizationTeamErrors.NameTaken);
        availability.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Create_Should_ReturnNameTaken_NotAnError_WhenTwoRequestsUseTheSameNameTogether()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        As(org.Owner.Id);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            Send(new CreateTeamCommand(org.Id, manager.Id, "Race", null))));

        // Assert
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Where(r => r.IsFailure).Should().OnlyContain(r => r.Error == OrganizationTeamErrors.NameTaken);
    }

    // ---- update, availability --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Update_Should_RenameTheTeam_ForItsManager_AndAllowACaseOnlyChange()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id, "marketing");
        As(managerUser.Id);

        // Act
        var caseOnly = await Send(new UpdateTeamCommand(org.Id, team.Id, "Marketing", null));
        var renamed = await Send(new UpdateTeamCommand(org.Id, team.Id, "Growth", "New focus"));

        // Assert
        caseOnly.IsSuccess.Should().BeTrue();
        renamed.IsSuccess.Should().BeTrue();
        var stored = await Stored(team.Id);
        stored.Name.Should().Be("Growth");
        stored.Description.Should().Be("New focus");
    }

    [Fact]
    public async Task Update_Should_ReturnNameTaken_ForAnotherTeamsName_AndRefuseAPlainMember()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id, "One");
        await Data.TeamAsync(org.Id, manager.Id, "Two");
        var (member, _) = await Data.MemberAsync(org.Id);

        // Act
        As(managerUser.Id);
        var taken = await Send(new UpdateTeamCommand(org.Id, team.Id, "Two", null));
        As(member.Id);
        var byMember = await Send(new UpdateTeamCommand(org.Id, team.Id, "Three", null));

        // Assert
        taken.Error.Should().Be(OrganizationTeamErrors.NameTaken);
        byMember.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        (await Stored(team.Id)).Name.Should().Be("One");
    }

    [Fact]
    public async Task CheckAvailability_Should_ReportTakenNames_AndExcludeTheTeamBeingRenamed()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id, "Sales");
        var (viewer, _) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        As(viewer.Id);

        // Act
        var taken = await Send(new CheckTeamNameAvailabilityCommand(org.Id, "Sales"));
        var own = await Send(new CheckTeamNameAvailabilityCommand(org.Id, "Sales", team.Id));
        var free = await Send(new CheckTeamNameAvailabilityCommand(org.Id, "Support"));

        // Assert
        taken.Value.Should().BeFalse();
        own.Value.Should().BeTrue();
        free.Value.Should().BeTrue();
    }

    // ---- members of a team ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task AddAndRemoveTeamMember_Should_ChangeTheTeamRoster()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        var (_, member) = await Data.MemberAsync(org.Id);
        As(managerUser.Id);

        // Act
        var added = await Send(new AddTeamMemberCommand(org.Id, team.Id, member.Id));
        var duplicate = await Send(new AddTeamMemberCommand(org.Id, team.Id, member.Id));
        var removed = await Send(new RemoveTeamMemberCommand(org.Id, team.Id, member.Id));

        // Assert
        added.IsSuccess.Should().BeTrue();
        duplicate.Error.Should().Be(OrganizationTeamErrors.AlreadyTeamMember);
        removed.IsSuccess.Should().BeTrue();
        (await IsOnTeam(team.Id, member.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task AddTeamMember_Should_Refuse_ARemovedMember_AndAMemberOfAnotherOrganization()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        var (_, removed) = await Data.MemberAsync(org.Id, isActive: false);
        var otherOrg = await Data.OrganizationAsync();
        As(managerUser.Id);

        // Act
        var removedResult = await Send(new AddTeamMemberCommand(org.Id, team.Id, removed.Id));
        var foreign = await Send(new AddTeamMemberCommand(org.Id, team.Id, otherOrg.OwnerMember.Id));

        // Assert
        removedResult.Error.Should().Be(OrganizationTeamErrors.MemberNotOrgMember);
        foreign.Error.Should().Be(OrganizationTeamErrors.MemberNotOrgMember);
    }

    [Fact]
    public async Task AddTeamMember_Should_ReturnAlreadyTeamMember_NotAnError_WhenRequestsArriveTogether()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        var (_, member) = await Data.MemberAsync(org.Id);
        As(managerUser.Id);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            Send(new AddTeamMemberCommand(org.Id, team.Id, member.Id))));

        // Assert
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Where(r => r.IsFailure).Should().OnlyContain(r => r.Error == OrganizationTeamErrors.AlreadyTeamMember);
    }

    [Fact]
    public async Task RemoveTeamMember_Should_RefuseToRemoveTheManager()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        As(managerUser.Id);

        // Act
        var result = await Send(new RemoveTeamMemberCommand(org.Id, team.Id, manager.Id));

        // Assert
        result.Error.Should().Be(OrganizationTeamErrors.CannotRemoveManager);
        (await IsOnTeam(team.Id, manager.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task TeamActions_Should_ReturnNotFound_ForATeamOfAnotherOrganization()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        var (otherOrg, _, otherManager) = await OrgWithManager();
        var team = await Data.TeamAsync(otherOrg.Id, otherManager.Id);
        As(org.Owner.Id);

        // Act / Assert
        (await Send(new UpdateTeamCommand(org.Id, team.Id, "X", null))).Error.Should().Be(OrganizationTeamErrors.NotFound);
        (await Send(new DeleteTeamCommand(org.Id, team.Id))).Error.Should().Be(OrganizationTeamErrors.NotFound);
        (await Send(new GetTeamQuery(org.Id, team.Id))).Error.Should().Be(OrganizationTeamErrors.NotFound);
    }

    // ---- manager change and deletion -----------------------------------------------------------------------------------------

    [Fact]
    public async Task ChangeManager_Should_AssignTheNewManager_AndPutThemOnTheTeam()
    {
        // Arrange
        var (org, _, oldManager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, oldManager.Id);
        var (_, newManager) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);
        As(org.Owner.Id);

        // Act
        var result = await Send(new ChangeTeamManagerCommand(org.Id, team.Id, newManager.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(team.Id)).TeamManagerId.Should().Be(newManager.Id);
        (await IsOnTeam(team.Id, newManager.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task ChangeManager_Should_Refuse_ALowRoleMember_AndTheCurrentManagerAsCaller()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        var (_, plain) = await Data.MemberAsync(org.Id);
        var (_, other) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);

        // Act
        As(org.Owner.Id);
        var lowRole = await Send(new ChangeTeamManagerCommand(org.Id, team.Id, plain.Id));
        As(managerUser.Id);
        var byManager = await Send(new ChangeTeamManagerCommand(org.Id, team.Id, other.Id));

        // Assert
        lowRole.Error.Should().Be(OrganizationTeamErrors.ManagerRoleTooLow);
        byManager.IsFailure.Should().BeTrue();
        (await Stored(team.Id)).TeamManagerId.Should().Be(manager.Id);
    }

    [Fact]
    public async Task Delete_Should_RemoveTheTeamAndItsMemberships_ForAnAdmin_AndRefuseItsManager()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        var (_, member) = await Data.MemberAsync(org.Id);
        await Data.TeamMemberAsync(team.Id, member.Id);

        // Act
        As(managerUser.Id);
        var byManager = await Send(new DeleteTeamCommand(org.Id, team.Id));
        As(org.Owner.Id);
        var byOwner = await Send(new DeleteTeamCommand(org.Id, team.Id));

        // Assert
        byManager.IsFailure.Should().BeTrue();
        byOwner.IsSuccess.Should().BeTrue();
        (await Query(db => db.Set<OrganizationTeam>().AnyAsync(t => t.Id == team.Id))).Should().BeFalse();
        (await Query(db => db.Set<OrganizationTeamMember>().AnyAsync(tm => tm.TeamId == team.Id))).Should().BeFalse();
    }

    // ---- reads -----------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetTeams_Should_ListByName_WithManagerAndMemberCount_ToAnyMember()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var b = await Data.TeamAsync(org.Id, manager.Id, "Beta");
        var a = await Data.TeamAsync(org.Id, manager.Id, "Alpha");
        var (_, member) = await Data.MemberAsync(org.Id);
        await Data.TeamMemberAsync(a.Id, member.Id);
        var (viewer, _) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        As(viewer.Id);

        // Act
        var result = (await Send(new GetOrganizationTeamsQuery(org.Id))).Value;
        var single = (await Send(new GetTeamQuery(org.Id, a.Id))).Value;

        // Assert
        result.Items.Select(t => t.Name).Should().Equal("Alpha", "Beta");
        result.TotalCount.Should().Be(2);
        result.Items[0].MemberCount.Should().Be(2);
        result.Items[0].TeamManagerUsername.Should().Be(managerUser.Username);
        single.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetTeams_Should_BeForMembersOnly()
    {
        // Arrange
        var (org, _, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        var outsider = await Data.UserAsync();
        As(outsider.Id);

        // Act / Assert
        (await Send(new GetOrganizationTeamsQuery(org.Id))).IsFailure.Should().BeTrue();
        (await Send(new GetTeamQuery(org.Id, team.Id))).IsFailure.Should().BeTrue();
        (await Send(new GetTeamMembersQuery(team.Id))).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GetTeamMembers_Should_ListTheRoster_OldestFirst()
    {
        // Arrange
        var (org, managerUser, manager) = await OrgWithManager();
        var team = await Data.TeamAsync(org.Id, manager.Id);
        var (memberUser, member) = await Data.MemberAsync(org.Id);
        await Task.Delay(10);
        await Data.TeamMemberAsync(team.Id, member.Id);
        As(managerUser.Id);

        // Act
        var result = (await Send(new GetTeamMembersQuery(team.Id))).Value;

        // Assert
        result.Items.Select(m => m.MemberId).Should().Equal(manager.Id, member.Id);
        result.Items[1].Username.Should().Be(memberUser.Username);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetManagedTeams_Should_ListTheTeamsTheCallerManages_AcrossActiveOrganizations()
    {
        // Arrange
        var user = await Data.UserAsync();
        var activeOrg = await Data.OrganizationAsync();
        var inactiveOrg = await Data.OrganizationAsync();
        var activeRow = OrganizationMember.Invite(activeOrg.Id, user.Id, MemberRole.TeamManager, activeOrg.Owner.Id, TestData.Now).Value;
        var inactiveRow = OrganizationMember.Invite(inactiveOrg.Id, user.Id, MemberRole.TeamManager, inactiveOrg.Owner.Id, TestData.Now).Value;
        await Seed(db => { db.AddRange(activeRow, inactiveRow); return Task.CompletedTask; });
        var managed = await Data.TeamAsync(activeOrg.Id, activeRow.Id, "Managed");
        await Data.TeamAsync(inactiveOrg.Id, inactiveRow.Id, "Hidden");
        await Data.TeamAsync(activeOrg.Id, activeOrg.OwnerMember.Id, "NotMine");
        await Seed(async db => (await db.Set<Organization>().SingleAsync(o => o.Id == inactiveOrg.Id)).Deactivate(TestData.Now));
        As(user.Id);

        // Act
        var result = (await Send(new GetMyManagedTeamsQuery())).Value;

        // Assert
        result.Select(t => t.Id).Should().Equal(managed.Id);
    }
}

internal static class MemberExtensions
{
    // The tests that act as a manager need the manager's user id, not the member row id.
    public static Guid UserId(this OrganizationMember member) => member.UserId;
}
