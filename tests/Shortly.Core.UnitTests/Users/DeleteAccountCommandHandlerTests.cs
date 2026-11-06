using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.UnitTests.TestData;
using Shortly.Core.Users.DeleteAccount;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Organizations;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Users;

public class DeleteAccountCommandHandlerTests
{
    private readonly User _user = UserData.Create();
    private readonly IOrganizationRepository _organizations = Substitute.For<IOrganizationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly DeleteAccountCommandHandler _sut;

    public DeleteAccountCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _sut = new DeleteAccountCommandHandler(CallerData.Create(_user.Id), _users, _organizations, _unitOfWork, TestClock.Provider(), _jwtService);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheCallerNoLongerExists()
    {
        // Arrange
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(new DeleteAccountCommand(), default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _jwtService.DidNotReceiveWithAnyArgs().RevokeAllForUserAsync(default, default);
    }

    [Fact]
    public async Task Handle_Should_SoftDeleteTheCaller_Save_AndRevokeEverySession()
    {
        // Act
        var result = await _sut.Handle(new DeleteAccountCommand(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.IsDeleted.Should().BeTrue();
        _user.IsActive.Should().BeFalse();
        _user.DeletedBy.Should().Be(_user.Id);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _jwtService.RevokeAllForUserAsync(_user.Id, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyDeleted_AndRevokeNothing_When_TheAccountIsAlreadyDeleted()
    {
        // Arrange
        _user.SoftDelete(_user.Id, TestClock.UtcNow);

        // Act
        var result = await _sut.Handle(new DeleteAccountCommand(), default);

        // Assert
        result.Error.Should().Be(UserErrors.AlreadyDeleted);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _jwtService.DidNotReceiveWithAnyArgs().RevokeAllForUserAsync(default, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnOwnsOrganizations_AndDeleteNothing_When_TheUserStillOwnsAnOrganization()
    {
        // Arrange
        _organizations.CountOwnedByAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _sut.Handle(new DeleteAccountCommand(), default);

        // Assert
        result.Error.Should().Be(UserErrors.OwnsOrganizations);
        _user.IsDeleted.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _jwtService.DidNotReceiveWithAnyArgs().RevokeAllForUserAsync(default, default);
    }
}
