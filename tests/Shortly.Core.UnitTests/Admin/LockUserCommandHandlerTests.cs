using Shortly.Core.Admin.LockUser;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class LockUserCommandHandlerTests
{
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly User _user = UserData.Create();
    private readonly IUserAuditLogRepository _auditLogs = Substitute.For<IUserAuditLogRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly LockUserCommandHandler _sut;

    public LockUserCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _sut = new LockUserCommandHandler(_users, _auditLogs, _unitOfWork, TestClock.Provider(), CallerData.Create(_adminId, isAdmin: true));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheUserDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new LockUserCommand(Guid.NewGuid(), "reason", TimeSpan.FromHours(1)), default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_LockTheUserUntilTheEndOfTheDuration_WithTheReason_AndSave()
    {
        // Act
        var result = await _sut.Handle(new LockUserCommand(_user.Id, "Abuse", TimeSpan.FromHours(2)), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.Security.LockedUntilUtc.Should().Be(TestClock.UtcNow.AddHours(2));
        _user.Security.LockoutReason.Should().Be("Abuse");
        _user.Security.IsLockedOut(TestClock.UtcNow).Should().BeTrue();
        _user.CanLogIn(TestClock.UtcNow).Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", 60, "LockReasonRequired")]
    [InlineData("   ", 60, "LockReasonRequired")]
    [InlineData("Abuse", 0, "InvalidLockDuration")]
    [InlineData("Abuse", -5, "InvalidLockDuration")]
    public async Task Handle_Should_ReturnTheDomainError_AndSaveNothing_ForABadReasonOrDuration(string reason, int minutes, string error)
    {
        // Act
        var result = await _sut.Handle(new LockUserCommand(_user.Id, reason, TimeSpan.FromMinutes(minutes)), default);

        // Assert
        result.Error.Should().Be(error == "LockReasonRequired" ? UserErrors.LockReasonRequired : UserErrors.InvalidLockDuration);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RecordAnAuditEntry_BeforeSaving()
    {
        // Act
        await _sut.Handle(new LockUserCommand(_user.Id, "Abuse", TimeSpan.FromHours(1)), default);

        // Assert
        _auditLogs.Received(1).Add(Arg.Is<UserAuditLog>(l =>
            l.UserId == _user.Id && l.Action == "AdminLock" && l.Details!.Contains(_adminId.ToString()) && l.Details.Contains("Abuse")
            && l.TimeStampUtc == TestClock.UtcNow));
        Received.InOrder(() =>
        {
            _auditLogs.Add(Arg.Any<UserAuditLog>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_NotRecordAnAuditEntry_WhenTheLockIsRejected()
    {
        // Act
        await _sut.Handle(new LockUserCommand(_user.Id, "", TimeSpan.FromHours(1)), default);

        // Assert
        _auditLogs.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotTargetSelf_When_TheAdministratorLocksThemselves()
    {
        // Act
        var result = await _sut.Handle(new LockUserCommand(_adminId, "oops", TimeSpan.FromHours(1)), default);

        // Assert
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
        await _users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
