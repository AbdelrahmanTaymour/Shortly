using Shortly.Domain.Users;

namespace Shortly.Domain.UnitTests.Users;

public class UserRolePermissionsTests
{
    [Theory]
    [InlineData(UserRole.User, enPermissions.BasicUser)]
    [InlineData(UserRole.SuperAdmin, enPermissions.SuperAdmin)]
    [InlineData(UserRole.SystemAdmin, enPermissions.SystemAdmin)]
    public void For_Should_MapEachRoleToItsPermissionSet(UserRole role, enPermissions expected)
    {
        // Act
        var permissions = UserRolePermissions.For(role);

        // Assert
        permissions.Should().Be(expected);
    }

    [Fact]
    public void For_Should_ReturnNoPermissions_ForAnUndefinedRole()
    {
        // Act
        var permissions = UserRolePermissions.For((UserRole)99);

        // Assert
        permissions.Should().Be(enPermissions.None);
    }

    [Theory]
    [InlineData(enPermissions.CreateLink)]
    [InlineData(enPermissions.ReadOwnLinks)]
    [InlineData(enPermissions.UpdateOwnLinks)]
    [InlineData(enPermissions.DeleteOwnLinks)]
    [InlineData(enPermissions.ReadOwnAnalytics)]
    [InlineData(enPermissions.ManageOwnAccount)]
    public void For_User_Should_IncludeThePersonalWorkspacePermissions(enPermissions expected)
    {
        // Act
        var permissions = UserRolePermissions.For(UserRole.User);

        // Assert
        permissions.HasFlag(expected).Should().BeTrue();
    }

    [Theory]
    [InlineData(enPermissions.ViewUsers)]
    [InlineData(enPermissions.DeleteUser)]
    [InlineData(enPermissions.DeleteOrganization)]
    [InlineData(enPermissions.ManageOrgBilling)]
    [InlineData(enPermissions.ReadUrls)]
    [InlineData(enPermissions.CleanupSystemData)]
    public void For_User_Should_NotIncludeAdministrationPermissions(enPermissions forbidden)
    {
        // Act
        var permissions = UserRolePermissions.For(UserRole.User);

        // Assert
        permissions.HasFlag(forbidden).Should().BeFalse();
    }

    [Fact]
    public void For_SuperAdmin_Should_IncludeEverythingTheBasicUserHas_AndUserAdministration()
    {
        // Act
        var permissions = UserRolePermissions.For(UserRole.SuperAdmin);

        // Assert
        permissions.HasFlag(enPermissions.BasicUser).Should().BeTrue();
        permissions.HasFlag(enPermissions.ViewUsers | enPermissions.DeleteUser | enPermissions.ReadUrls).Should().BeTrue();
    }

    [Fact]
    public void For_SuperAdmin_Should_NotIncludeSystemCleanup()
    {
        // Act
        var permissions = UserRolePermissions.For(UserRole.SuperAdmin);

        // Assert
        permissions.HasFlag(enPermissions.CleanupSystemData).Should().BeFalse();
    }

    [Fact]
    public void For_SystemAdmin_Should_HaveEveryDefinedPermission()
    {
        // Arrange
        var everyDefined = Enum.GetValues<enPermissions>().Aggregate(enPermissions.None, (all, p) => all | p);

        // Act
        var permissions = UserRolePermissions.For(UserRole.SystemAdmin);

        // Assert
        permissions.HasFlag(everyDefined).Should().BeTrue();
    }
}
