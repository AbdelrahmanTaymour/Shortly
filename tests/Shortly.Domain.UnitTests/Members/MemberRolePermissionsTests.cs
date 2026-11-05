using Shortly.Domain.Members;
using Shortly.Domain.Users;

namespace Shortly.Domain.UnitTests.Members;

public class MemberRolePermissionsTests
{
    [Theory]
    [InlineData(MemberRole.Member, enPermissions.TeamMember)]
    [InlineData(MemberRole.TeamManager, enPermissions.TeamManager)]
    [InlineData(MemberRole.OrgAdmin, enPermissions.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner, enPermissions.OrgOwner)]
    public void For_Should_MapEachRoleToItsPermissionSet(MemberRole role, enPermissions expected)
    {
        // Act
        var permissions = MemberRolePermissions.For(role);

        // Assert
        permissions.Should().Be(expected);
    }

    [Fact]
    public void For_Viewer_Should_BeLimitedToReadingLinksAndAnalytics()
    {
        // Act
        var permissions = MemberRolePermissions.For(MemberRole.Viewer);

        // Assert
        permissions.Should().Be(enPermissions.ReadOwnAnalytics | enPermissions.ReadOrgAnalytics |
                                enPermissions.ReadTeamAnalytics | enPermissions.ReadOrgLinks);
    }

    [Theory]
    [InlineData(enPermissions.CreateLink)]
    [InlineData(enPermissions.UpdateOwnLinks)]
    [InlineData(enPermissions.DeleteOwnLinks)]
    [InlineData(enPermissions.UpdateOrgLinks)]
    [InlineData(enPermissions.DeleteOrgLinks)]
    [InlineData(enPermissions.ManageOrgMembers)]
    [InlineData(enPermissions.CreateInvitations)]
    [InlineData(enPermissions.UpdateOrganization)]
    [InlineData(enPermissions.ManageOrgBilling)]
    public void For_Viewer_Should_NotHaveAnyWritePermission(enPermissions forbidden)
    {
        // Act
        var permissions = MemberRolePermissions.For(MemberRole.Viewer);

        // Assert
        permissions.HasFlag(forbidden).Should().BeFalse();
    }

    [Theory]
    [InlineData(enPermissions.CreateLink)]
    [InlineData(enPermissions.UpdateOwnLinks)]
    [InlineData(enPermissions.DeleteOwnLinks)]
    public void For_Member_Should_ManageTheirOwnLinks(enPermissions expected)
    {
        // Act
        var permissions = MemberRolePermissions.For(MemberRole.Member);

        // Assert
        permissions.HasFlag(expected).Should().BeTrue();
    }

    [Theory]
    [InlineData(enPermissions.ManageOrgMembers)]
    [InlineData(enPermissions.CreateInvitations)]
    [InlineData(enPermissions.UpdateOrganization)]
    [InlineData(enPermissions.ManageOrgBilling)]
    [InlineData(enPermissions.RemoveMembers)]
    public void For_Member_Should_NotManageTheOrganization(enPermissions forbidden)
    {
        // Act
        var permissions = MemberRolePermissions.For(MemberRole.Member);

        // Assert
        permissions.HasFlag(forbidden).Should().BeFalse();
    }

    [Theory]
    [InlineData(enPermissions.CreateInvitations)]
    [InlineData(enPermissions.ManageInvitations)]
    [InlineData(enPermissions.RemoveMembers)]
    [InlineData(enPermissions.UpdateOrganization)]
    public void For_OrgAdmin_Should_ManageMembersInvitationsAndSettings(enPermissions expected)
    {
        // Act
        var permissions = MemberRolePermissions.For(MemberRole.OrgAdmin);

        // Assert
        permissions.HasFlag(expected).Should().BeTrue();
    }

    [Theory]
    [InlineData(enPermissions.TransferOrgOwnership)]
    [InlineData(enPermissions.DeleteOrganization)]
    public void For_OrgAdmin_Should_NotTransferOwnershipOrDeleteTheOrganization(enPermissions forbidden)
    {
        // Act
        var permissions = MemberRolePermissions.For(MemberRole.OrgAdmin);

        // Assert
        permissions.HasFlag(forbidden).Should().BeFalse();
    }

    [Theory]
    [InlineData(enPermissions.TransferOrgOwnership)]
    [InlineData(enPermissions.DeleteOrganization)]
    [InlineData(enPermissions.ManageOrgBilling)]
    public void For_OrgOwner_Should_HaveTheOwnerOnlyPermissions(enPermissions expected)
    {
        // Act
        var permissions = MemberRolePermissions.For(MemberRole.OrgOwner);

        // Assert
        permissions.HasFlag(expected).Should().BeTrue();
    }

    [Fact]
    public void For_Should_GiveEachHigherRoleEverythingTheLowerOnesHave_FromMemberUp()
    {
        // Arrange
        var member = MemberRolePermissions.For(MemberRole.Member);
        var manager = MemberRolePermissions.For(MemberRole.TeamManager);
        var admin = MemberRolePermissions.For(MemberRole.OrgAdmin);
        var owner = MemberRolePermissions.For(MemberRole.OrgOwner);

        // Assert
        manager.HasFlag(member).Should().BeTrue();
        admin.HasFlag(manager).Should().BeTrue();
        owner.HasFlag(admin).Should().BeTrue();
    }

    [Fact]
    public void For_Should_ReturnNoPermissions_ForAnUndefinedRole()
    {
        // Act
        var permissions = MemberRolePermissions.For((MemberRole)99);

        // Assert
        permissions.Should().Be(enPermissions.None);
    }

    // ---- alignment with the business rules ---------------------------------------------------------------------------

    [Fact]
    public void MemberRole_Should_DefineFiveRoles_IncludingTeamManager()
    {
        // Assert: TeamManager is a deliberate fifth operational role between Member and Admin
        Enum.GetValues<MemberRole>().Should().BeEquivalentTo(
            [MemberRole.Viewer, MemberRole.Member, MemberRole.TeamManager, MemberRole.OrgAdmin, MemberRole.OrgOwner]);
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public void For_Should_LetEveryRoleViewOrganizationLinksAndAnalytics(MemberRole role)
    {
        // Act
        var permissions = MemberRolePermissions.For(role);

        // Assert
        permissions.HasFlag(enPermissions.ReadOrgLinks).Should().BeTrue();
        permissions.HasFlag(enPermissions.ReadOrgAnalytics).Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.OrgAdmin)]
    [InlineData(MemberRole.OrgOwner)]
    public void For_Should_LetAdminsAndOwnersManageAllOrganizationLinksAndBilling(MemberRole role)
    {
        // Act
        var permissions = MemberRolePermissions.For(role);

        // Assert
        permissions.HasFlag(enPermissions.UpdateOrgLinks | enPermissions.DeleteOrgLinks).Should().BeTrue();
        permissions.HasFlag(enPermissions.ManageOrgBilling).Should().BeTrue();
    }

    [Theory]
    [InlineData(MemberRole.Viewer)]
    [InlineData(MemberRole.Member)]
    [InlineData(MemberRole.TeamManager)]
    public void For_Should_NotLetRolesBelowAdminEditOrDeleteOthersLinksOrManageBilling(MemberRole role)
    {
        // Act
        var permissions = MemberRolePermissions.For(role);

        // Assert
        permissions.HasFlag(enPermissions.UpdateOrgLinks).Should().BeFalse();
        permissions.HasFlag(enPermissions.DeleteOrgLinks).Should().BeFalse();
        permissions.HasFlag(enPermissions.ManageOrgBilling).Should().BeFalse();
    }

    [Fact]
    public void For_Should_BeStrictlyMonotonic_FromViewerToOwner()
    {
        // Arrange
        var viewer = MemberRolePermissions.For(MemberRole.Viewer);
        var member = MemberRolePermissions.For(MemberRole.Member);
        var manager = MemberRolePermissions.For(MemberRole.TeamManager);
        var admin = MemberRolePermissions.For(MemberRole.OrgAdmin);
        var owner = MemberRolePermissions.For(MemberRole.OrgOwner);

        // Assert: each role has everything the one below has, and strictly more
        member.HasFlag(viewer).Should().BeTrue();
        manager.HasFlag(member).Should().BeTrue();
        admin.HasFlag(manager).Should().BeTrue();
        owner.HasFlag(admin).Should().BeTrue();

        member.Should().NotBe(viewer);
        manager.Should().NotBe(member);
        admin.Should().NotBe(manager);
        owner.Should().NotBe(admin);
    }
}
