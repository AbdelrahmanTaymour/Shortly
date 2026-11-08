using Microsoft.EntityFrameworkCore;
using Shortly.Core.Admin.LockUser;
using Shortly.Core.Admin.UnlockUser;
using Shortly.Core.Exceptions;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Admin;

public class UserLockingTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<User> Stored(Guid id) => Query(db => db.Set<User>().Include(u => u.Security).SingleAsync(u => u.Id == id));
    private Task<List<UserAuditLog>> AuditLogOf(Guid id) => Query(db => db.Set<UserAuditLog>().Where(l => l.UserId == id).ToListAsync());

    // ---- lock ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task LockUser_Should_LockTheAccount_AndRecordAnAuditEntry()
    {
        // Arrange
        var target = await Data.UserAsync();
        var admin = await Data.UserAsync();
        As(admin.Id);

        // Act
        var result = await Send(new LockUserCommand(target.Id, "Suspicious activity", TimeSpan.FromHours(2)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stored = await Stored(target.Id);
        stored.Security.IsLockedOut(TestData.Now).Should().BeTrue();
        var log = (await AuditLogOf(target.Id)).Should().ContainSingle(l => l.Action == "AdminLock").Which;
        log.Details.Should().Contain("Suspicious activity").And.Contain(admin.Id.ToString());
    }

    [Fact]
    public async Task LockUser_Should_RefuseToLockTheCallersOwnAccount()
    {
        // Arrange
        var admin = await Data.UserAsync();
        As(admin.Id);

        // Act
        var result = await Send(new LockUserCommand(admin.Id, "Self-lock attempt", TimeSpan.FromHours(1)));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
        (await Stored(admin.Id)).Security.IsLockedOut(TestData.Now).Should().BeFalse();
    }

    [Fact]
    public async Task LockUser_Should_Fail_ForAUserThatDoesNotExist()
    {
        // Arrange
        As(Caller.Admin());

        // Act
        var result = await Send(new LockUserCommand(Guid.NewGuid(), "Reason", TimeSpan.FromHours(1)));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task LockUser_Should_RejectAZeroOrNegativeDuration_BeforeTouchingTheUser()
    {
        // Arrange
        var target = await Data.UserAsync();
        As(Caller.Admin());

        // Act
        var act = () => Send(new LockUserCommand(target.Id, "Reason", TimeSpan.Zero));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        (await Stored(target.Id)).Security.IsLockedOut(TestData.Now).Should().BeFalse();
    }

    // ---- unlock -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task UnlockUser_Should_ClearTheLockout_AndRecordAnAuditEntry()
    {
        // Arrange
        var target = await Data.UserAsync();
        var admin = await Data.UserAsync();
        As(admin.Id);
        (await Send(new LockUserCommand(target.Id, "Reason", TimeSpan.FromHours(1)))).IsSuccess.Should().BeTrue();

        // Act
        var result = await Send(new UnlockUserCommand(target.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(target.Id)).Security.IsLockedOut(TestData.Now).Should().BeFalse();
        (await AuditLogOf(target.Id)).Should().Contain(l => l.Action == "AdminUnlock");
    }

    [Fact]
    public async Task UnlockUser_Should_Fail_ForAUserThatDoesNotExist()
    {
        // Arrange
        As(Caller.Admin());

        // Act
        var result = await Send(new UnlockUserCommand(Guid.NewGuid()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }
}