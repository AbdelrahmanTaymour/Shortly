using Shortly.Core.Admin.BulkActivateUsers;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class BulkActivateUsersCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkActivateUsersCommandHandler _sut;

    public BulkActivateUsersCommandHandlerTests()
    {
        _sut = new BulkActivateUsersCommandHandler(_users, _unitOfWork, TestClock.Provider());
    }

    private User Existing(bool active = false)
    {
        var user = UserData.Create(email: $"{Guid.NewGuid():N}@example.com", username: Guid.NewGuid().ToString("N"));
        if (!active) user.Deactivate(TestClock.UtcNow);
        return user;
    }

    private void Returns(params User[] users) =>
        _users.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(users);

    [Fact]
    public async Task Handle_Should_ActivateEveryUser_AndSaveOnce()
    {
        // Arrange
        var a = Existing(active: false);
        var b = Existing(active: false);
        Returns(a, b);

        // Act
        var result = await _sut.Handle(new BulkActivateUsersCommand([a.Id, b.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 2, 0));
        a.IsActive.Should().BeTrue();
        b.IsActive.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountUnknownIdsAsFailures_AndStillSaveTheRest()
    {
        // Arrange
        var known = Existing(active: false);
        Returns(known);

        // Act
        var result = await _sut.Handle(new BulkActivateUsersCommand([known.Id, Guid.NewGuid()]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountUsersThatCannotChangeAsFailures()
    {
        // Arrange
        var blocked = Existing(active: true);
        var ok = Existing(active: false);
        Returns(blocked, ok);

        // Act
        var result = await _sut.Handle(new BulkActivateUsersCommand([blocked.Id, ok.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
    }

    [Fact]
    public async Task Handle_Should_NotSave_When_NothingSucceeds()
    {
        // Arrange
        var blocked = Existing(active: true);
        Returns(blocked);

        // Act
        var result = await _sut.Handle(new BulkActivateUsersCommand([blocked.Id, Guid.NewGuid()]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 0, 2));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountARepeatedIdOnlyOnceAsASuccess()
    {
        // Arrange: the second occurrence finds the user already changed
        var user = Existing(active: false);
        Returns(user);

        // Act
        var result = await _sut.Handle(new BulkActivateUsersCommand([user.Id, user.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
    }
}
