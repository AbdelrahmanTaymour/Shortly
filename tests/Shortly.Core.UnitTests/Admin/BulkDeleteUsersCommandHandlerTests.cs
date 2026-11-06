using Shortly.Core.Admin.BulkDeleteUsers;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class BulkDeleteUsersCommandHandlerTests
{
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BulkDeleteUsersCommandHandler _sut;

    public BulkDeleteUsersCommandHandlerTests()
    {
        _sut = new BulkDeleteUsersCommandHandler(_users, _organizations, CallerData.Create(_adminId, isAdmin: true), _unitOfWork, TestClock.Provider());
    }

    private static User Existing(bool deleted = false)
    {
        var user = UserData.Create(email: $"{Guid.NewGuid():N}@example.com", username: Guid.NewGuid().ToString("N"));
        if (deleted) user.SoftDelete(Guid.NewGuid(), TestClock.UtcNow);
        return user;
    }

    private void Returns(params User[] users) =>
        _users.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns(users);

    [Fact]
    public async Task Handle_Should_SoftDeleteEveryUser_RecordingTheCaller_AndSaveOnce()
    {
        // Arrange
        var a = Existing();
        var b = Existing();
        Returns(a, b);

        // Act
        var result = await _sut.Handle(new BulkDeleteUsersCommand([a.Id, b.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 2, 0));
        a.IsDeleted.Should().BeTrue();
        a.IsActive.Should().BeFalse();
        a.DeletedBy.Should().Be(_adminId);
        a.DeletedAtUtc.Should().Be(TestClock.UtcNow);
        b.IsDeleted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CountUnknownAndAlreadyDeletedUsersAsFailures()
    {
        // Arrange
        var live = Existing();
        var gone = Existing(deleted: true);
        Returns(live, gone);

        // Act
        var result = await _sut.Handle(new BulkDeleteUsersCommand([live.Id, gone.Id, Guid.NewGuid()]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(3, 1, 2));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotSave_When_NothingSucceeds()
    {
        // Arrange
        Returns();

        // Act
        var result = await _sut.Handle(new BulkDeleteUsersCommand([Guid.NewGuid()]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(1, 0, 1));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RefuseToDeleteTheCaller_AndCountItAsAFailure()
    {
        // Arrange
        var self = UserData.CreateWithId(_adminId);
        var other = Existing();
        Returns(self, other);

        // Act
        var result = await _sut.Handle(new BulkDeleteUsersCommand([_adminId, other.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        self.IsDeleted.Should().BeFalse();
        other.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_RefuseToDeleteAUserWhoOwnsAnOrganization_AndCountItAsAFailure()
    {
        // Arrange
        var owner = Existing();
        var other = Existing();
        Returns(owner, other);
        _organizations.CountOwnedByAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(new BulkDeleteUsersCommand([owner.Id, other.Id]), default);

        // Assert
        result.Value.Should().Be(new BulkOperationResponse(2, 1, 1));
        owner.IsDeleted.Should().BeFalse();
        other.IsDeleted.Should().BeTrue();
    }
}
