using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.BulkActivateUsers;
using Shortly.Core.Admin.BulkDeactivateUsers;
using Shortly.Core.Admin.BulkDeleteUsers;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class BulkUserOperationsTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<User> Stored(Guid id) => Query(db => db.Set<User>().SingleAsync(u => u.Id == id));

    // ---- bulk activate ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task BulkActivateUsers_Should_ActivateEveryInactiveUser_AndReportAccurateCounts()
    {
        // Arrange
        var inactiveOne = await Data.UserAsync(isActive: false);
        var inactiveTwo = await Data.UserAsync(isActive: false);
        var missingId = Guid.NewGuid();
        As(Caller.Admin());

        // Act
        var result = await Send(new BulkActivateUsersCommand([inactiveOne.Id, inactiveTwo.Id, missingId]));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalProcessed.Should().Be(3);
        result.Value.SuccessCount.Should().Be(2);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(inactiveOne.Id)).IsActive.Should().BeTrue();
        (await Stored(inactiveTwo.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task BulkActivateUsers_Should_CountAnAlreadyActiveUser_AsAFailure()
    {
        // Arrange: Activate() rejects a user that's already active
        var active = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var result = await Send(new BulkActivateUsersCommand([active.Id]));

        // Assert
        result.Value.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(1);
    }

    // ---- bulk deactivate --------------------------------------------------------------------------------------------

    [Fact]
    public async Task BulkDeactivateUsers_Should_DeactivateEveryTargetedUser()
    {
        // Arrange
        var userOne = await Data.UserAsync();
        var userTwo = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var result = await Send(new BulkDeactivateUsersCommand([userOne.Id, userTwo.Id]));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuccessCount.Should().Be(2);
        (await Stored(userOne.Id)).IsActive.Should().BeFalse();
        (await Stored(userTwo.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task BulkDeactivateUsers_Should_SkipTheCallersOwnId_AsAFailure_ButStillProcessTheRest()
    {
        // Arrange
        var admin = await Data.UserAsync();
        var other = await Data.UserAsync();
        As(admin.Id);

        // Act
        var result = await Send(new BulkDeactivateUsersCommand([admin.Id, other.Id]));

        // Assert
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(admin.Id)).IsActive.Should().BeTrue();
        (await Stored(other.Id)).IsActive.Should().BeFalse();
    }

    // ---- bulk delete (soft) -------------------------------------------------------------------------------------------
    
    [Fact]
    public async Task BulkDeleteUsers_Should_SoftDeleteEveryTargetedUser()
    {
        // Arrange
        var admin = await Data.UserAsync();
        As(Caller.Admin(admin.Id));

        var owner = (await Data.OrganizationAsync()).Owner;
        var ordinary = await Data.UserAsync();

        // Act
        var result = await Send(new BulkDeleteUsersCommand([owner.Id, ordinary.Id]));

        // Assert
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(owner.Id)).IsDeleted.Should().BeFalse();
        (await Stored(ordinary.Id)).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BulkDeleteUsers_Should_SkipAUserThatStillOwnsAnOrganization_AsAFailure()
    {
        // Arrange
        var admin = await Data.UserAsync();
        As(Caller.Admin(admin.Id));
    
        var owner = (await Data.OrganizationAsync()).Owner;
        var ordinary = await Data.UserAsync();

        // Act
        var result = await Send(new BulkDeleteUsersCommand([owner.Id, ordinary.Id]));

        // Assert
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(owner.Id)).IsDeleted.Should().BeFalse();
        (await Stored(ordinary.Id)).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BulkDeleteUsers_Should_SkipTheCallersOwnId_AsAFailure()
    {
        // Arrange
        var admin = await Data.UserAsync();
        As(admin.Id);

        // Act
        var result = await Send(new BulkDeleteUsersCommand([admin.Id]));

        // Assert
        result.Value.SuccessCount.Should().Be(0);
        result.Value.FailureCount.Should().Be(1);
        (await Stored(admin.Id)).IsDeleted.Should().BeFalse();
    }
}