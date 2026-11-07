using Microsoft.EntityFrameworkCore;
using Shortly.Core.Exceptions;
using Shortly.Domain.Members;
using Shortly.Domain.Organizations;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Persistence;

public class OrganizationRepositoryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task Change(Guid organizationId, Action<Organization> change) => Seed(async db =>
        change(await db.Set<Organization>().SingleAsync(o => o.Id == organizationId)));

    // ---- organizations -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_Should_LoadTheOrganizationWithItsUsage()
    {
        // Arrange
        var org = await Data.OrganizationAsync();

        // Act
        var loaded = await WithRepository<IOrganizationRepository, Organization?>(r => r.GetByIdAsync(org.Id), save: false);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(org.Organization.Name);
        loaded.Usage.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_HideASoftDeletedOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        await Change(org.Id, o => o.SoftDelete(TestData.Now));

        // Act
        var loaded = await WithRepository<IOrganizationRepository, Organization?>(r => r.GetByIdAsync(org.Id), save: false);

        // Assert
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task CountOwnedByAsync_Should_CountOnlyTheUsersOwnNonDeletedOrganizations()
    {
        // Arrange
        var owner = await Data.UserAsync();
        await Data.OrganizationAsync(owner);
        await Data.OrganizationAsync(owner);
        var deleted = await Data.OrganizationAsync(owner);
        await Change(deleted.Id, o => o.SoftDelete(TestData.Now));
        await Data.OrganizationAsync(); // someone else's

        // Act
        var count = await WithRepository<IOrganizationRepository, int>(r => r.CountOwnedByAsync(owner.Id), save: false);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task CountOwnedByAsync_Should_CountAnInactiveOrganization_ButNotATransferredOne()
    {
        // Arrange
        var owner = await Data.UserAsync();
        var inactive = await Data.OrganizationAsync(owner);
        await Change(inactive.Id, o => o.Deactivate(TestData.Now));
        var transferred = await Data.OrganizationAsync(owner);
        var newOwner = await Data.UserAsync();
        await Change(transferred.Id, o => o.TransferOwnership(newOwner.Id, TestData.Now));

        // Act
        var count = await WithRepository<IOrganizationRepository, int>(r => r.CountOwnedByAsync(owner.Id), save: false);

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public async Task IsOwnerAsync_Should_BeTrueOnlyForTheOwner_OfALiveOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var deleted = await Data.OrganizationAsync();
        await Change(deleted.Id, o => o.SoftDelete(TestData.Now));

        // Act / Assert
        (await WithRepository<IOrganizationRepository, bool>(r => r.IsOwnerAsync(org.Id, org.Owner.Id), save: false)).Should().BeTrue();
        (await WithRepository<IOrganizationRepository, bool>(r => r.IsOwnerAsync(org.Id, Guid.NewGuid()), save: false)).Should().BeFalse();
        (await WithRepository<IOrganizationRepository, bool>(r => r.IsOwnerAsync(deleted.Id, deleted.Owner.Id), save: false)).Should().BeFalse();
    }

    [Fact]
    public async Task Saving_Should_Fail_ForASecondOrganizationWithTheSameOwnerAndName()
    {
        // Arrange
        var owner = await Data.UserAsync();
        await Data.OrganizationAsync(owner, name: "Acme Inc");

        // Act
        var duplicate = () => Data.OrganizationAsync(owner, name: "Acme Inc");
        var otherOwner = () => Data.OrganizationAsync(name: "Acme Inc");

        // Assert
        await duplicate.Should().ThrowAsync<UniqueConstraintViolationException>();
        await otherOwner.Should().NotThrowAsync();
    }

    // ---- members -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GetByOrganizationAndUserAsync_Should_ReturnTheRow_EvenWhenTheMemberWasRemoved()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, _) = await Data.MemberAsync(org.Id, isActive: false);

        // Act
        var member = await WithRepository<IOrganizationMemberRepository, OrganizationMember?>(
            r => r.GetByOrganizationAndUserAsync(org.Id, user.Id), save: false);

        // Assert
        member.Should().NotBeNull();
        member!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveByOrganizationAndUserAsync_Should_RequireAnActiveMember_OfAnActiveLiveOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (active, _) = await Data.MemberAsync(org.Id);
        var (removed, _) = await Data.MemberAsync(org.Id, isActive: false);
        var inactiveOrg = await Data.OrganizationAsync();
        var (inInactive, _) = await Data.MemberAsync(inactiveOrg.Id);
        await Change(inactiveOrg.Id, o => o.Deactivate(TestData.Now));
        var deletedOrg = await Data.OrganizationAsync();
        var (inDeleted, _) = await Data.MemberAsync(deletedOrg.Id);
        await Change(deletedOrg.Id, o => o.SoftDelete(TestData.Now));

        // Act
        Task<OrganizationMember?> Get(Guid orgId, Guid userId) =>
            WithRepository<IOrganizationMemberRepository, OrganizationMember?>(r => r.GetActiveByOrganizationAndUserAsync(orgId, userId), save: false);

        // Assert
        (await Get(org.Id, active.Id)).Should().NotBeNull();
        (await Get(org.Id, removed.Id)).Should().BeNull();
        (await Get(inactiveOrg.Id, inInactive.Id)).Should().BeNull();
        (await Get(deletedOrg.Id, inDeleted.Id)).Should().BeNull();
        (await Get(org.Id, Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task IsMemberOfOrganizationAsync_Should_FollowTheSameRules()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (active, _) = await Data.MemberAsync(org.Id);
        var (removed, _) = await Data.MemberAsync(org.Id, isActive: false);
        var deletedOrg = await Data.OrganizationAsync();
        var (inDeleted, _) = await Data.MemberAsync(deletedOrg.Id);
        await Change(deletedOrg.Id, o => o.SoftDelete(TestData.Now));

        // Act / Assert
        (await WithRepository<IOrganizationMemberRepository, bool>(r => r.IsMemberOfOrganizationAsync(active.Id, org.Id), save: false)).Should().BeTrue();
        (await WithRepository<IOrganizationMemberRepository, bool>(r => r.IsMemberOfOrganizationAsync(removed.Id, org.Id), save: false)).Should().BeFalse();
        (await WithRepository<IOrganizationMemberRepository, bool>(r => r.IsMemberOfOrganizationAsync(inDeleted.Id, deletedOrg.Id), save: false)).Should().BeFalse();
    }

    [Fact]
    public async Task GetMemberCountByOrganizationAsync_Should_CountActiveMembers_IncludingTheOwner()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        await Data.MemberAsync(org.Id);
        await Data.MemberAsync(org.Id, MemberRole.Viewer);
        await Data.MemberAsync(org.Id, isActive: false);

        // Act
        var count = await WithRepository<IOrganizationMemberRepository, int>(r => r.GetMemberCountByOrganizationAsync(org.Id), save: false);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task Saving_Should_Fail_ForTheSameUserTwiceInOneOrganization()
    {
        // Arrange
        var org = await Data.OrganizationAsync();
        var (user, _) = await Data.MemberAsync(org.Id);

        // Act
        var duplicate = () => WithRepository<IOrganizationMemberRepository, bool>(r =>
        {
            r.Add(OrganizationMember.Invite(org.Id, user.Id, MemberRole.Viewer, org.Owner.Id, TestData.Now).Value);
            return true;
        });

        // Assert
        await duplicate.Should().ThrowAsync<UniqueConstraintViolationException>();
    }
}
