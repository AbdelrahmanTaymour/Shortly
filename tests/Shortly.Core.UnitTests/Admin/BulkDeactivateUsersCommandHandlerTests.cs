using Shortly.Core.Admin.BulkDeactivateUsers;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class BulkDeactivateUsersCommandHandlerTests
{
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkDeactivateUsersCommandHandler _sut;

    public BulkDeactivateUsersCommandHandlerTests()
    {
        _sut = new BulkDeactivateUsersCommandHandler(_users, CallerData.Create(_adminId, isAdmin: true), _unitOfWork, TestClock.Provider());
    }

    private User Existing(bool active = true)
    {
        var user = UserData.Create(email: $"{Guid.NewGuid():N}@example.com", username: Guid.NewGuid().ToString("N"));
        if (!active) user.Deactivate(TestClock.UtcNow);
        return user;
    }

    private void Returns(params User[] users) =>
        _users.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(users);

    [Fact]
    public async Task Handle_Should_DeactivateEveryUser_AndSaveOnce()
    {
        // Arrange
        var a = Existing(active: true);
        var b = Existing(active: true);
        Returns(a, b);

        // Act
        var result = await _sut.Handle(new BulkDeactivateUsersCommand([a.Id, b.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 2, 0));
        a.IsActive.Should().BeFalse();
        b.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountUnknownIdsAsFailures_AndStillSaveTheRest()
    {
        // Arrange
        var known = Existing(active: true);
        Returns(known);

        // Act
        var result = await _sut.Handle(new BulkDeactivateUsersCommand([known.Id, Guid.NewGuid()]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountUsersThatCannotChangeAsFailures()
    {
        // Arrange
        var blocked = Existing(active: false);
        var ok = Existing(active: true);
        Returns(blocked, ok);

        // Act
        var result = await _sut.Handle(new BulkDeactivateUsersCommand([blocked.Id, ok.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
    }

    [Fact]
    public async Task Handle_Should_NotSave_When_NothingSucceeds()
    {
        // Arrange
        var blocked = Existing(active: false);
        Returns(blocked);

        // Act
        var result = await _sut.Handle(new BulkDeactivateUsersCommand([blocked.Id, Guid.NewGuid()]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 0, 2));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountARepeatedIdOnlyOnceAsASuccess()
    {
        // Arrange: the second occurrence finds the user already changed
        var user = Existing(active: true);
        Returns(user);

        // Act
        var result = await _sut.Handle(new BulkDeactivateUsersCommand([user.Id, user.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
    }

    [Fact]
    public async Task Handle_Should_RefuseToDeactivateTheCaller_AndCountItAsAFailure()
    {
        // Arrange
        var self = UserData.CreateWithId(_adminId);
        var other = Existing(active: true);
        Returns(self, other);

        // Act
        var result = await _sut.Handle(new BulkDeactivateUsersCommand([self.Id, other.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        self.IsActive.Should().BeTrue();
        other.IsActive.Should().BeFalse();
    }
}
