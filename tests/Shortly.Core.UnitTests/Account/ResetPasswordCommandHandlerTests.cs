using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.ResetPassword;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class ResetPasswordCommandHandlerTests
{
    private static readonly ResetPasswordCommand Command = new("plain-token", "N3w-password!");

    private readonly User _user = UserData.Create();
    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        _tokenHasher.Hash(Command.Token).Returns(TokenData.Hash);
        _passwordHasher.Hash(Command.NewPassword).Returns("new-hash");
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _sut = new ResetPasswordCommandHandler(
            _tokens, _users, _passwordHasher, _tokenHasher, _jwtService, _unitOfWork, TestClock.Provider());
    }

    private UserActionToken TokenInTheRepository(UserActionToken token)
    {
        _tokens.GetByTokenHashAsync(TokenData.Hash, Arg.Any<CancellationToken>()).Returns(token);
        return token;
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_NoTokenMatches()
    {
        // Arrange
        _tokens.GetByTokenHashAsync(TokenData.Hash, Arg.Any<CancellationToken>()).Returns((UserActionToken?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnWrongType_When_TheTokenIsNotAPasswordResetToken()
    {
        // Arrange
        TokenInTheRepository(TokenData.EmailVerification(_user.Id));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.WrongType);
        _user.PasswordHash.Should().Be(UserData.PasswordHash);
    }

    [Fact]
    public async Task Handle_Should_ReturnUsed_AndKeepThePassword_When_TheTokenWasAlreadyUsed()
    {
        // Arrange
        TokenInTheRepository(TokenData.Used(TokenData.PasswordReset(_user.Id)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Used);
        _user.PasswordHash.Should().Be(UserData.PasswordHash);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnExpired_AndKeepThePassword_When_TheTokenIsOlderThanOneHour()
    {
        // Arrange: issued two hours ago, valid for one hour
        TokenInTheRepository(TokenData.PasswordReset(_user.Id, TestClock.UtcNow.AddHours(-2)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Expired);
        _user.PasswordHash.Should().Be(UserData.PasswordHash);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTokenOwnerNoLongerExists()
    {
        // Arrange
        TokenInTheRepository(TokenData.PasswordReset(_user.Id));
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ChangeThePassword_ConsumeTheToken_AndSave()
    {
        // Arrange
        var token = TokenInTheRepository(TokenData.PasswordReset(_user.Id));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.PasswordHash.Should().Be("new-hash");
        token.Used.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ClearALockout_AndSignTheUserOutEverywhere_AfterSaving()
    {
        // Arrange
        TokenInTheRepository(TokenData.PasswordReset(_user.Id));
        for (var i = 0; i < 3; i++) _user.RecordFailedLogin(TestClock.UtcNow, 3, TimeSpan.FromMinutes(15));

        // Act
        await _sut.Handle(Command, default);

        // Assert
        _user.Security.IsLockedOut(TestClock.UtcNow).Should().BeFalse();
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _jwtService.RevokeAllForUserAsync(_user.Id, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_NotAllowTheSameTokenTwice()
    {
        // Arrange
        TokenInTheRepository(TokenData.PasswordReset(_user.Id));
        await _sut.Handle(Command, default);

        // Act
        var second = await _sut.Handle(Command, default);

        // Assert
        second.Error.Should().Be(UserActionTokenErrors.Used);
    }
}
