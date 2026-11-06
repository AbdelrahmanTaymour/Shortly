using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.ChangePassword;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class ChangePasswordCommandHandlerTests
{
    private static readonly ChangePasswordCommand Command = new("current-password", "N3w-password!");

    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _passwordHasher.Verify(Command.CurrentPassword, UserData.PasswordHash).Returns(true);
        _passwordHasher.Hash(Command.NewPassword).Returns("new-hash");
        _sut = new ChangePasswordCommandHandler(
            CallerData.Create(_user.Id), _users, _passwordHasher, _jwtService, _unitOfWork, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheCallerNoLongerExists()
    {
        // Arrange
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_AndChangeNothing_When_TheCurrentPasswordIsWrong()
    {
        // Arrange
        _passwordHasher.Verify(Command.CurrentPassword, UserData.PasswordHash).Returns(false);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidCredentials);
        _user.PasswordHash.Should().Be(UserData.PasswordHash);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _jwtService.DidNotReceiveWithAnyArgs().RevokeAllForUserAsync(default, default);
    }

    [Fact]
    public async Task Handle_Should_StoreTheHashOfTheNewPassword_AndSave()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.PasswordHash.Should().Be("new-hash");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SignTheUserOutEverywhere_AfterTheNewPasswordWasSaved()
    {
        // Act
        await _sut.Handle(Command, default);

        // Assert
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _jwtService.RevokeAllForUserAsync(_user.Id, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_ClearAnActiveLockout_When_ThePasswordIsChanged()
    {
        // Arrange
        for (var i = 0; i < 3; i++) _user.RecordFailedLogin(TestClock.UtcNow, 3, TimeSpan.FromMinutes(15));

        // Act
        await _sut.Handle(Command, default);

        // Assert
        _user.Security.FailedLoginAttempts.Should().Be(0);
        _user.Security.IsLockedOut(TestClock.UtcNow).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSaveNothing_When_TheNewHashIsBlank()
    {
        // Arrange
        _passwordHasher.Hash(Command.NewPassword).Returns(string.Empty);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.PasswordRequired);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
