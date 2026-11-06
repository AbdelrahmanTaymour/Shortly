using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.VerifyEmail;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class VerifyEmailCommandHandlerTests
{
    private static readonly VerifyEmailCommand Command = new("plain-token");
    private static readonly AccessTokenResponse Tokens = new("access-token", "refresh-token", 900);

    private readonly User _user = UserData.Create();
    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly VerifyEmailCommandHandler _sut;

    public VerifyEmailCommandHandlerTests()
    {
        _tokenHasher.Hash(Command.Token).Returns(TokenData.Hash);
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(Tokens);
        _sut = new VerifyEmailCommandHandler(_tokens, _users, _tokenHasher, _jwtService, _unitOfWork, TestClock.Provider());
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
    public async Task Handle_Should_ReturnWrongType_When_TheTokenIsNotAnEmailVerificationToken()
    {
        // Arrange
        TokenInTheRepository(TokenData.PasswordReset(_user.Id));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.WrongType);
    }

    [Fact]
    public async Task Handle_Should_ReturnUsed_When_TheTokenWasAlreadyUsed()
    {
        // Arrange
        TokenInTheRepository(TokenData.Used(TokenData.EmailVerification(_user.Id)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Used);
    }

    [Fact]
    public async Task Handle_Should_ReturnExpired_When_TheTokenIsOlderThan24Hours()
    {
        // Arrange
        TokenInTheRepository(TokenData.EmailVerification(_user.Id, TestClock.UtcNow.AddHours(-25)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Expired);
        _user.IsEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTokenOwnerNoLongerExists()
    {
        // Arrange
        TokenInTheRepository(TokenData.EmailVerification(_user.Id));
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyConfirmed_AndLeaveTheTokenUnused_When_TheEmailIsAlreadyConfirmed()
    {
        // Arrange
        var confirmed = UserData.CreateConfirmed();
        var token = TokenInTheRepository(TokenData.EmailVerification(confirmed.Id));
        _users.GetByIdAsync(confirmed.Id, Arg.Any<CancellationToken>()).Returns(confirmed);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailAlreadyConfirmed);
        token.Used.Should().BeFalse();
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ConfirmTheEmail_ConsumeTheToken_Save_AndSignTheUserIn()
    {
        // Arrange
        var token = TokenInTheRepository(TokenData.EmailVerification(_user.Id));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().Be(Tokens);
        _user.IsEmailConfirmed.Should().BeTrue();
        token.Used.Should().BeTrue();
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _jwtService.IssueTokensAsync(_user, Arg.Any<CancellationToken>());
        });
    }
}
