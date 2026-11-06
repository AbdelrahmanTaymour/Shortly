using Microsoft.EntityFrameworkCore;
using Shortly.Core.Members.AddMember;
using Shortly.Core.Members.GetMembership;
using Shortly.Core.Members.GetOrganizationMembers;
using Shortly.Core.Members.LeaveOrganization;
using Shortly.Core.Members.RemoveMember;
using Shortly.Core.Members.UpdateMemberRole;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.Teams;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Organizations;

public class MemberManagementTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<OrganizationMember> Stored(Guid orgId, Guid userId) =>
        Query(db => db.Set<OrganizationMember>().SingleAsync(m => m.OrganizationId == orgId && m.UserId == userId));

    private Task<int> ActiveCount(Guid orgId) =>
        Query(db => db.Set<OrganizationMember>().CountAsync(m => m.OrganizationId == orgId && m.IsActive));

    // ---- add -------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Add_Should_CreateAnActiveMember_WhoWasInvitedByTheCaller()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var user = await Data.UserAsync();
        As(org.Owner.Id);

        // Act
        var result = await Send(new AddMemberCommand(org.Id, user.Id, MemberRole.Viewer));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var member = await Stored(org.Id, user.Id);
        member.Role.Should().Be(MemberRole.Viewer);
        member.IsActive.Should().BeTrue();
        member.InvitedBy.Should().Be(org.Owner.Id);
    }

    [Fact]
    public async Task Add_Should_ReactivateARemovedMember_InTheSameRow()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, row) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin, isActive: false);
        As(org.Owner.Id);

        // Act
        var result = await Send(new AddMemberCommand(org.Id, user.Id, MemberRole.Viewer));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var member = await Stored(org.Id, user.Id);
        member.Id.Should().Be(row.Id);
        member.IsActive.Should().BeTrue();
        member.Role.Should().Be(MemberRole.Viewer);
        (await Query(db => db.Set<OrganizationMember>().CountAsync(m => m.OrganizationId == org.Id && m.UserId == user.Id))).Should().Be(1);
    }

    [Fact]
    public async Task Add_Should_Refuse_AMember_AnOwnerRole_AnUnknownUser_AndADuplicate()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (existing, _) = await Data.MemberAsync(org.Id);
        var fresh = await Data.UserAsync();

        // Act
        As(existing.Id);
        var byMember = await Send(new AddMemberCommand(org.Id, fresh.Id, MemberRole.Member));
        As(admin.Id);
        var asOwner = await Send(new AddMemberCommand(org.Id, fresh.Id, MemberRole.OrgOwner));
        var unknown = await Send(new AddMemberCommand(org.Id, Guid.NewGuid(), MemberRole.Member));
        var duplicate = await Send(new AddMemberCommand(org.Id, existing.Id, MemberRole.Member));

        // Assert
        byMember.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        asOwner.Error.Should().Be(OrganizationMemberErrors.CannotGrantOwnerRoleDirectly);
        unknown.Error.Should().Be(UserErrors.NotFound);
        duplicate.Error.Should().Be(OrganizationMemberErrors.AlreadyMember);
    }

    [Fact]
    public async Task Add_Should_StopAtTheMemberLimit()
    {
        // Arrange: a Free organization allows 3 members and already has the owner
        var org = await Data.OrganizationAsync(memberLimit: 3);
        await Data.MemberAsync(org.Id);
        var a = await Data.UserAsync();
        var b = await Data.UserAsync();
        As(org.Owner.Id);

        // Act
        var last = await Send(new AddMemberCommand(org.Id, a.Id, MemberRole.Member));
        var beyond = await Send(new AddMemberCommand(org.Id, b.Id, MemberRole.Member));

        // Assert
        last.IsSuccess.Should().BeTrue();
        beyond.Error.Should().Be(OrganizationMemberErrors.MemberLimitReached);
        (await ActiveCount(org.Id)).Should().Be(3);
    }

    [Fact]
    public async Task Add_Should_NeverExceedTheMemberLimit_WhenRequestsArriveTogether()
    {
        // Arrange: two free slots, eight users added at the same moment
        var org = await Data.OrganizationAsync(memberLimit: 3);
        var users = new List<User>();
        for (var i = 0; i < 8; i++) users.Add(await Data.UserAsync());
        As(org.Owner.Id);

        // Act
        var results = await Task.WhenAll(users.Select(async u =>
        {
            try { return (await Send(new AddMemberCommand(org.Id, u.Id, MemberRole.Member))).IsSuccess; }
            catch (Exception) { return false; }
        }));

        // Assert
        (await ActiveCount(org.Id)).Should().Be(3);
        results.Count(ok => ok).Should().Be(2);
    }

    // ---- role changes ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task UpdateRole_Should_PersistTheNewRole_ForAnAdmin()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (member, _) = await Data.MemberAsync(org.Id);
        As(admin.Id);

        // Act
        var result = await Send(new UpdateMemberRoleCommand(org.Id, member.Id, MemberRole.TeamManager));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(org.Id, member.Id)).Role.Should().Be(MemberRole.TeamManager);
    }

    [Fact]
    public async Task UpdateRole_Should_Refuse_AMember_TheOwnerRole_AndAChangeToTheOwner()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (member, _) = await Data.MemberAsync(org.Id);

        // Act
        As(member.Id);
        var byMember = await Send(new UpdateMemberRoleCommand(org.Id, admin.Id, MemberRole.Viewer));
        As(admin.Id);
        var toOwner = await Send(new UpdateMemberRoleCommand(org.Id, member.Id, MemberRole.OrgOwner));
        var demoteOwner = await Send(new UpdateMemberRoleCommand(org.Id, org.Owner.Id, MemberRole.Viewer));

        // Assert
        byMember.IsFailure.Should().BeTrue();
        toOwner.IsFailure.Should().BeTrue();
        demoteOwner.IsFailure.Should().BeTrue();
        (await Stored(org.Id, org.Owner.Id)).Role.Should().Be(MemberRole.OrgOwner);
        (await Stored(org.Id, admin.Id)).Role.Should().Be(MemberRole.OrgAdmin);
    }

    [Fact]
    [Trait("Finding", "I-09")]
    public async Task UpdateRole_Should_LetAnAdminDemoteATeamManager_WhoStillManagesATeam_CurrentBehavior()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (manager, managerRow) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);
        await Data.TeamAsync(org.Id, managerRow.Id);
        As(org.Owner.Id);

        // Act
        var result = await Send(new UpdateMemberRoleCommand(org.Id, manager.Id, MemberRole.Viewer));

        // Assert: a Viewer now "manages" a team, which creating or reassigning a team would refuse
        result.IsSuccess.Should().BeTrue();
        (await Stored(org.Id, manager.Id)).Role.Should().Be(MemberRole.Viewer);
    }

    // ---- removal and leaving -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Remove_Should_DeactivateTheMember_KeepTheRow_AndDropTheirTeamMemberships()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (manager, managerRow) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);
        var (member, memberRow) = await Data.MemberAsync(org.Id);
        var team = await Data.TeamAsync(org.Id, managerRow.Id);
        await Data.TeamMemberAsync(team.Id, memberRow.Id);
        As(org.Owner.Id);

        // Act
        var result = await Send(new RemoveMemberCommand(org.Id, member.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(org.Id, member.Id)).IsActive.Should().BeFalse();
        (await Query(db => db.Set<OrganizationTeamMember>().AnyAsync(tm => tm.MemberId == memberRow.Id))).Should().BeFalse();
        (await Query(db => db.Set<OrganizationTeamMember>().AnyAsync(tm => tm.MemberId == managerRow.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task Remove_Should_Refuse_TheOwner_AManagerOfATeam_AndANonAdmin()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (manager, managerRow) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);
        await Data.TeamAsync(org.Id, managerRow.Id);
        var (member, _) = await Data.MemberAsync(org.Id);
        var (other, _) = await Data.MemberAsync(org.Id);

        // Act
        As(org.Owner.Id);
        var owner = await Send(new RemoveMemberCommand(org.Id, org.Owner.Id));
        var managing = await Send(new RemoveMemberCommand(org.Id, manager.Id));
        As(member.Id);
        var byMember = await Send(new RemoveMemberCommand(org.Id, other.Id));

        // Assert
        owner.Error.Should().Be(OrganizationMemberErrors.CannotRemoveOwner);
        managing.Error.Should().Be(OrganizationTeamErrors.MemberManagesTeams);
        byMember.Error.Should().Be(OrganizationMemberErrors.InsufficientRole);
        (await Stored(org.Id, manager.Id)).IsActive.Should().BeTrue();
        (await Stored(org.Id, other.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Leave_Should_DeactivateTheCallersMembership_AndReturnPersonalTokens()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, memberRow) = await Data.MemberAsync(org.Id);
        var team = await Data.TeamAsync(org.Id, org.OwnerMember.Id);
        await Data.TeamMemberAsync(team.Id, memberRow.Id);
        As(member.Id, org.Id);

        // Act
        var result = await Send(new LeaveOrganizationCommand(org.Id));

        // Assert
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        (await Stored(org.Id, member.Id)).IsActive.Should().BeFalse();
        (await Query(db => db.Set<OrganizationTeamMember>().AnyAsync(tm => tm.MemberId == memberRow.Id))).Should().BeFalse();
    }

    [Fact]
    public async Task Leave_Should_Refuse_TheOwner_AndAManagerOfATeam()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (manager, managerRow) = await Data.MemberAsync(org.Id, MemberRole.TeamManager);
        await Data.TeamAsync(org.Id, managerRow.Id);

        // Act
        As(org.Owner.Id);
        var owner = await Send(new LeaveOrganizationCommand(org.Id));
        As(manager.Id);
        var managing = await Send(new LeaveOrganizationCommand(org.Id));

        // Assert
        owner.Error.Should().Be(OrganizationMemberErrors.OwnerCannotLeave);
        managing.Error.Should().Be(OrganizationTeamErrors.MemberManagesTeams);
    }

    [Fact]
    public async Task ARemovedMember_Should_LoseAccessToTheOrganizationsData_Immediately()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);
        As(member.Id, org.Id);
        (await Send(new GetOrganizationMembersQuery(org.Id))).IsSuccess.Should().BeTrue();
        As(org.Owner.Id);
        await Send(new RemoveMemberCommand(org.Id, member.Id));

        // Act: the old token still names the organization
        As(member.Id, org.Id);
        var result = await Send(new GetOrganizationMembersQuery(org.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    // ---- reads -------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetMembers_Should_ListEveryMemberRow_OldestFirst_WithTheirDetails()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (a, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        await Task.Delay(10);
        var (b, _) = await Data.MemberAsync(org.Id, isActive: false);
        As(a.Id);

        // Act
        var result = (await Send(new GetOrganizationMembersQuery(org.Id))).Value;

        // Assert
        result.Items.Select(i => i.UserId).Should().Equal(org.Owner.Id, a.Id, b.Id);
        result.TotalCount.Should().Be(3);
        result.Items[0].Role.Should().Be(MemberRole.OrgOwner);
        result.Items[0].Email.Should().Be(org.Owner.Email);
        result.Items[2].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetMembers_Should_PageTheResults()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        for (var i = 0; i < 4; i++) { await Data.MemberAsync(org.Id); await Task.Delay(5); }
        As(org.Owner.Id);

        // Act
        var page2 = (await Send(new GetOrganizationMembersQuery(org.Id, 2, 2))).Value;

        // Assert
        page2.Items.Should().HaveCount(2);
        page2.TotalCount.Should().Be(5);
        page2.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetMembers_Should_BeForMembersOnly()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var outsider = await Data.UserAsync();
        As(outsider.Id);

        // Act
        var result = await Send(new GetOrganizationMembersQuery(org.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-4, 20, 1, 20)]
    [InlineData(1, 0, 1, 20)]
    [InlineData(1, 5000, 1, 20)]
    public async Task GetMembers_Should_ReplaceOutOfRangePaging_InsteadOfFailing(int page, int pageSize, int expectedPage, int expectedSize)
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(org.Owner.Id);

        // Act
        var result = await Send(new GetOrganizationMembersQuery(org.Id, page, pageSize));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(expectedPage);
        result.Value.PageSize.Should().Be(expectedSize);
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMembership_Should_ReturnTheMemberRow_ToAnyMember_AndNotFoundForAStranger()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (viewer, _) = await Data.MemberAsync(org.Id, MemberRole.Viewer);
        As(viewer.Id);

        // Act
        var owner = await Send(new GetMembershipQuery(org.Id, org.Owner.Id));
        var stranger = await Send(new GetMembershipQuery(org.Id, Guid.NewGuid()));

        // Assert
        owner.Value.Role.Should().Be(MemberRole.OrgOwner);
        owner.Value.Username.Should().Be(org.Owner.Username);
        stranger.Error.Should().Be(OrganizationMemberErrors.NotFound);
    }
}
