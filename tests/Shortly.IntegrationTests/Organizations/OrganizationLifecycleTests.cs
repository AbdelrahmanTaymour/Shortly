using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Core.Organizations.ActivateOrganization;
using Shortly.Core.Organizations.CreateOrganization;
using Shortly.Core.Organizations.DeactivateOrganization;
using Shortly.Core.Organizations.DeleteOrganization;
using Shortly.Core.Organizations.ExitOrganization;
using Shortly.Core.Organizations.GetMyOrganizations;
using Shortly.Core.Organizations.GetOrganizationUsage;
using Shortly.Core.Organizations.SwitchOrganization;
using Shortly.Core.Organizations.TransferOwnership;
using Shortly.Core.Organizations.UpdateOrganization;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.Domain.SubscriptionPlan;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Organizations;

public class OrganizationLifecycleTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static CreateOrganizationCommand Create(string? name = null) =>
        new(name ?? $"Org {Guid.NewGuid():N}", "About us", "https://acme.dev", "https://acme.dev/logo.png");

    private Task<Organization> Stored(Guid id) => Query(db => db.Set<Organization>().Include(o => o.Usage).SingleAsync(o => o.Id == id));

    private Task<OrganizationMember> StoredMember(Guid orgId, Guid userId) =>
        Query(db => db.Set<OrganizationMember>().SingleAsync(m => m.OrganizationId == orgId && m.UserId == userId));

    private async Task<Guid> CreatedOrganizationId(Guid ownerId) =>
        await Query(db => db.Set<Organization>().Where(o => o.OwnerId == ownerId).Select(o => o.Id).SingleAsync());

    // ---- create ----------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_Should_StoreAFreeOrganization_WithTheCallerAsOwner_AndItsUsageRow_AndReturnTokens()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var result = await Send(Create("Acme Inc"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        var org = await Stored(await CreatedOrganizationId(user.Id));
        org.Name.Should().Be("Acme Inc");
        org.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Free);
        org.IsSubscribed.Should().BeFalse();
        org.IsActive.Should().BeTrue();
        org.MemberLimit.Should().Be(3);
        org.Usage.MonthlyLinksCreated.Should().Be(0);
        var owner = await StoredMember(org.Id, user.Id);
        owner.Role.Should().Be(MemberRole.OrgOwner);
        owner.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_Should_RefuseASecondOrganization_ToAFreeUser_ButNotToAnEnterpriseUser()
    {
        // Arrange
        var free = await Data.UserAsync();
        var enterprise = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);

        // Act
        As(free.Id);
        await Send(Create());
        var second = await Send(Create());
        As(enterprise.Id);
        for (var i = 0; i < 3; i++) (await Send(Create())).IsSuccess.Should().BeTrue();

        // Assert
        second.Error.Should().Be(OrganizationErrors.OrganizationLimitReached);
        (await Query(db => db.Set<Organization>().CountAsync(o => o.OwnerId == free.Id))).Should().Be(1);
        (await Query(db => db.Set<Organization>().CountAsync(o => o.OwnerId == enterprise.Id))).Should().Be(3);
    }

    [Fact]
    public async Task Create_Should_NotCountADeletedOrganization_TowardTheLimit()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);
        await Send(Create());
        await Send(new DeleteOrganizationCommand(await CreatedOrganizationId(user.Id)));

        // Act
        var again = await Send(Create());

        // Assert
        again.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_Should_RejectABlankName_BeforeStoringAnything()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var act = () => Send(Create("   "));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        (await Query(db => db.Set<Organization>().CountAsync(o => o.OwnerId == user.Id))).Should().Be(0);
    }

    [Fact]
    public async Task Create_Should_NeverExceedTheOrganizationLimit_WhenRequestsArriveTogether()
    {
        // Arrange: a Free user may own one organization
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            try { return await Send(Create()); }
            catch (Exception) { return Shortly.Domain.Abstractions.Result.Failure<Shortly.Core.Abstractions.Authentication.AccessTokenResponse>(OrganizationErrors.OrganizationLimitReached); }
        }));

        // Assert
        (await Query(db => db.Set<Organization>().CountAsync(o => o.OwnerId == user.Id))).Should().Be(1);
        results.Count(r => r.IsSuccess).Should().Be(1);
    }

    // ---- update ----------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Update_Should_PersistTheChanges_ForTheOwner_AndKeepOmittedFields()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        As(org.Owner.Id);

        // Act
        var result = await Send(new UpdateOrganizationCommand(org.Id, "  New   name ", null, "https://new.dev", null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(org.Id);
        stored.Name.Should().Be("New name");
        stored.Website.Should().Be("https://new.dev");
        stored.MemberLimit.Should().Be(org.Organization.MemberLimit);
    }

    [Fact]
    public async Task Update_Should_RefuseAnAdmin_AndAMember()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (member, _) = await Data.MemberAsync(org.Id);

        // Act
        As(admin.Id);
        var asAdmin = await Send(new UpdateOrganizationCommand(org.Id, "Hijacked", null, null, null));
        As(member.Id);
        var asMember = await Send(new UpdateOrganizationCommand(org.Id, "Hijacked", null, null, null));

        // Assert
        asAdmin.Error.Should().Be(OrganizationErrors.NotOwner);
        asMember.Error.Should().Be(OrganizationErrors.NotOwner);
        (await Stored(org.Id)).Name.Should().Be(org.Organization.Name);
    }

    [Fact]
    public async Task Update_Should_ReturnNameTaken_ForARenameToAnotherOrganizationOfTheSameOwner()
    {
        // Arrange
        var owner = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        await Data.OrganizationAsync(owner, name: "Taken name");
        var second = await Data.OrganizationAsync(owner, name: "Second");
        As(owner.Id);

        // Act
        var result = await Send(new UpdateOrganizationCommand(second.Id, "Taken name", null, null, null));

        // Assert
        result.Error.Should().Be(OrganizationErrors.NameTaken);
        (await Stored(second.Id)).Name.Should().Be("Second");
    }

    [Fact]
    public async Task Create_Should_ReturnNameTaken_ForADuplicateName_AndLetADifferentOwnerUseIt()
    {
        // Arrange
        var first = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var second = await Data.UserAsync();
        As(first.Id);
        await Send(Create("Acme Dup"));

        // Act
        var duplicate = await Send(Create("Acme Dup"));
        As(second.Id);
        var otherOwner = await Send(Create("Acme Dup"));

        // Assert
        duplicate.Error.Should().Be(OrganizationErrors.NameTaken);
        otherOwner.IsSuccess.Should().BeTrue();
        (await Query(db => db.Set<Organization>().CountAsync(o => o.OwnerId == first.Id))).Should().Be(1);
    }

    // ---- activation and deletion -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Deactivate_Should_BlockMembersAndLinks_UntilTheOwnerActivatesAgain()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);

        // Act
        As(org.Owner.Id);
        var deactivated = await Send(new DeactivateOrganizationCommand(org.Id));
        As(member.Id);
        var switchWhileInactive = await Send(new SwitchOrganizationCommand(org.Id));
        As(org.Owner.Id);
        var activated = await Send(new ActivateOrganizationCommand(org.Id));
        As(member.Id);
        var switchAfter = await Send(new SwitchOrganizationCommand(org.Id));

        // Assert
        deactivated.IsSuccess.Should().BeTrue();
        switchWhileInactive.Error.Should().Be(OrganizationErrors.NotAMember);
        activated.IsSuccess.Should().BeTrue();
        switchAfter.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAndActivate_Should_BeForTheOwnerOnly()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        As(admin.Id);

        // Act
        var deactivate = await Send(new DeactivateOrganizationCommand(org.Id));
        var activate = await Send(new ActivateOrganizationCommand(org.Id));

        // Assert
        deactivate.IsFailure.Should().BeTrue();
        activate.IsFailure.Should().BeTrue();
        (await Stored(org.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Should_RefuseAnAdmin_AndKeepTheOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        As(admin.Id);

        // Act
        var result = await Send(new DeleteOrganizationCommand(org.Id));

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotOwner);
        (await Stored(org.Id)).DeletedAt.Should().BeNull();
    }

    // ---- ownership transfer ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Transfer_Should_SwapTheRoles_AndTheOwnerReference()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (newOwner, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        As(org.Owner.Id);

        // Act
        var result = await Send(new TransferOwnershipCommand(org.Id, newOwner.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(org.Id)).OwnerId.Should().Be(newOwner.Id);
        (await StoredMember(org.Id, newOwner.Id)).Role.Should().Be(MemberRole.OrgOwner);
        (await StoredMember(org.Id, org.Owner.Id)).Role.Should().Be(MemberRole.Member);
    }

    [Fact]
    public async Task Transfer_Should_Refuse_ANonOwner_ANonMember_AndARemovedMember()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (removed, _) = await Data.MemberAsync(org.Id, isActive: false);
        var outsider = await Data.UserAsync();

        // Act
        As(admin.Id);
        var byAdmin = await Send(new TransferOwnershipCommand(org.Id, admin.Id));
        As(org.Owner.Id);
        var toOutsider = await Send(new TransferOwnershipCommand(org.Id, outsider.Id));
        var toRemoved = await Send(new TransferOwnershipCommand(org.Id, removed.Id));

        // Assert
        byAdmin.Error.Should().Be(OrganizationErrors.NotOwner);
        toOutsider.Error.Should().Be(OrganizationErrors.NewOwnerNotMember);
        toRemoved.Error.Should().Be(OrganizationErrors.NewOwnerNotMember);
        (await Stored(org.Id)).OwnerId.Should().Be(org.Owner.Id);
    }

    [Fact]
    public async Task Transfer_Should_MakeTheNewOwnerCountTowardTheirOrganizationLimit()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (newOwner, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        As(org.Owner.Id);
        await Send(new TransferOwnershipCommand(org.Id, newOwner.Id));

        // Act: the new owner (a Free user who now owns one organization) cannot create another
        As(newOwner.Id);
        var create = await Send(Create());

        // Assert
        create.Error.Should().Be(OrganizationErrors.OrganizationLimitReached);
    }

    // ---- switching ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Switch_Should_IssueTokens_ForAnActiveMember_AndRefuseOthers()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (member, _) = await Data.MemberAsync(org.Id);
        var (removed, _) = await Data.MemberAsync(org.Id, isActive: false);
        var outsider = await Data.UserAsync();

        // Act
        As(member.Id);
        var asMember = await Send(new SwitchOrganizationCommand(org.Id));
        As(removed.Id);
        var asRemoved = await Send(new SwitchOrganizationCommand(org.Id));
        As(outsider.Id);
        var asOutsider = await Send(new SwitchOrganizationCommand(org.Id));

        // Assert
        asMember.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        asRemoved.Error.Should().Be(OrganizationErrors.NotAMember);
        asOutsider.Error.Should().Be(OrganizationErrors.NotAMember);
    }

    [Fact]
    public async Task Exit_Should_IssuePersonalWorkspaceTokens()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var result = await Send(new ExitOrganizationCommand());

        // Assert
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    // ---- reads -------------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetMyOrganizations_Should_ListActiveMemberships_ByName_AndSkipRemovedOrDeletedOnes()
    {
        // Arrange
        var user = await Data.UserAsync(plan: enSubscriptionPlan.Enterprise);
        var zeta = await Data.OrganizationAsync(user, name: $"Zeta {Guid.NewGuid():N}");
        var alpha = await Data.OrganizationAsync(user, name: $"Alpha {Guid.NewGuid():N}");
        var other = await Data.OrganizationAsync();
        var removedFrom = await Data.OrganizationAsync();
        var deleted = await Data.OrganizationAsync(user);
        await Seed(async db => (await db.Set<Organization>().SingleAsync(o => o.Id == deleted.Id)).SoftDelete(TestData.Now));
        await Seed(db =>
        {
            db.Add(OrganizationMember.Invite(other.Id, user.Id, MemberRole.Viewer, other.Owner.Id, TestData.Now).Value);
            var removed = OrganizationMember.Invite(removedFrom.Id, user.Id, MemberRole.Member, removedFrom.Owner.Id, TestData.Now).Value;
            removed.Deactivate();
            db.Add(removed);
            return Task.CompletedTask;
        });
        As(user.Id);

        // Act
        var result = (await Send(new GetMyOrganizationsQuery())).Value;

        // Assert
        result.Select(o => o.OrganizationId).Should().Equal(alpha.Id, other.Id, zeta.Id);
        result.Single(o => o.OrganizationId == other.Id).Role.Should().Be(MemberRole.Viewer);
        result.Single(o => o.OrganizationId == alpha.Id).Role.Should().Be(MemberRole.OrgOwner);
    }

    [Fact]
    public async Task GetUsage_Should_BeForAdminsAndOwners_AndShowThePlanQuota()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (admin, _) = await Data.MemberAsync(org.Id, MemberRole.OrgAdmin);
        var (member, _) = await Data.MemberAsync(org.Id);
        await Seed(db => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE organization_usage SET monthly_links_created = 2 WHERE organization_id = {org.Id}"));

        // Act
        As(admin.Id);
        var asAdmin = await Send(new GetOrganizationUsageQuery(org.Id));
        As(member.Id);
        var asMember = await Send(new GetOrganizationUsageQuery(org.Id));
        As(Caller.Admin());
        var asSystemAdmin = await Send(new GetOrganizationUsageQuery(org.Id));

        // Assert
        asAdmin.Value.MonthlyLinksCreated.Should().Be(2);
        asAdmin.Value.MonthlyLinksLimit.Should().Be(5);
        asAdmin.Value.RemainingLinks.Should().Be(3);
        asAdmin.Value.CanCreateMoreLinks.Should().BeTrue();
        asMember.IsFailure.Should().BeTrue();
        asSystemAdmin.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsage_Should_ReturnNotFound_ForAnInactiveOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        await Seed(async db => (await db.Set<Organization>().SingleAsync(o => o.Id == org.Id)).Deactivate(TestData.Now));
        As(Caller.Admin());

        // Act
        var result = await Send(new GetOrganizationUsageQuery(org.Id));

        // Assert
        result.Error.Should().Be(OrganizationErrors.NotFound);
    }
}
