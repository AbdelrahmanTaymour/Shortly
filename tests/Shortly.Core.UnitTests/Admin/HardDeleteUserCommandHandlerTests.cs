using Shortly.Core.Admin.HardDeleteUser;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.ShortUrls;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Admin;

public class HardDeleteUserCommandHandlerTests
{
    private readonly User _user = UserData.Create();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IShortUrlRepository _shortUrls = Substitute.For<IShortUrlRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransaction _transaction = Substitute.For<ITransaction>();
    private readonly HardDeleteUserCommandHandler _sut;

    public HardDeleteUserCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);
        _sut = new HardDeleteUserCommandHandler(CallerData.Create(_adminId, isAdmin: true), _users, _organizations, _shortUrls, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_AndOpenNoTransaction_When_TheUserDoesNotExist()
    {
        // Act
        var result = await _sut.Handle(new HardDeleteUserCommand(Guid.NewGuid()), default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteTheUsersLinks_RemoveTheUser_AndCommit_()
    {
        // Act
        var result = await _sut.Handle(new HardDeleteUserCommand(_user.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _shortUrls.Received(1).DeleteAllForUserAsync(_user.Id, Arg.Any<CancellationToken>());
        _users.Received(1).Remove(_user);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_RollBack_AndRethrow_When_SavingFails()
    {
        // Arrange
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<Task<int>>(_ => throw new InvalidOperationException("db"));

        // Act
        var act = () => _sut.Handle(new HardDeleteUserCommand(_user.Id), default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnOwnsOrganizations_AndDeleteNothing_When_TheUserOwnsAnOrganization()
    {
        // Arrange
        _organizations.CountOwnedByAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(new HardDeleteUserCommand(_user.Id), default);

        // Assert
        result.Error.Should().Be(UserErrors.OwnsOrganizations);
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        _users.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnCannotTargetSelf_When_TheAdministratorDeletesThemselves()
    {
        // Act
        var result = await _sut.Handle(new HardDeleteUserCommand(_adminId), default);

        // Assert
        result.Error.Should().Be(UserErrors.CannotTargetSelf);
        await _users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }
}
