using Shortly.Core.Admin.UnlockUser;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class UnlockUserCommandHandlerTests
{
    private readonly User _user = UserData.CreateLocked();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly IUserAuditLogRepository _auditLogs = Substitute.For<IUserAuditLogRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UnlockUserCommandHandler _sut;

    public UnlockUserCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _sut = new UnlockUserCommandHandler(_users, _auditLogs, _unitOfWork, TestClock.Provider(), CallerData.Create(_adminId, isAdmin: true));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheUserDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new UnlockUserCommand(Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ClearTheLock_TheReason_AndTheFailedAttempts_AndSave()
    {
        // Arrange
        _user.Security.IsLockedOut(TestClock.UtcNow).Should().BeTrue();

        // Act
        var result = await _sut.Handle(new UnlockUserCommand(_user.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.Security.IsLockedOut(TestClock.UtcNow).Should().BeFalse();
        _user.Security.LockoutReason.Should().BeNull();
        _user.Security.FailedLoginAttempts.Should().Be(0);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Succeed_ForAUserThatIsNotLocked()
    {
        // Arrange
        var free = UserData.Create(email: "free@example.com", username: "free");
        _users.GetByIdAsync(free.Id, Arg.Any<CancellationToken>()).Returns(free);

        // Act
        var result = await _sut.Handle(new UnlockUserCommand(free.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_RecordAnAuditEntry_BeforeSaving()
    {
        // Act
        await _sut.Handle(new UnlockUserCommand(_user.Id), default);

        // Assert
        _auditLogs.Received(1).Add(Arg.Is<UserAuditLog>(l =>
            l.UserId == _user.Id && l.Action == "AdminUnlock" && l.Details!.Contains(_adminId.ToString())));
        Received.InOrder(() =>
        {
            _auditLogs.Add(Arg.Any<UserAuditLog>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }
}
