using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.ConfirmEmailChange;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class ConfirmEmailChangeCommandHandlerTests
{
    private const string NewEmail = "new@example.com";
    private static readonly ConfirmEmailChangeCommand Command = new("plain-token");

    private readonly User _user = UserData.Create();
    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfirmEmailChangeCommandHandler _sut;

    public ConfirmEmailChangeCommandHandlerTests()
    {
        _tokenHasher.Hash(Command.Token).Returns(TokenData.Hash);
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _sut = new ConfirmEmailChangeCommandHandler(_tokens, _users, _tokenHasher, _unitOfWork, TestClock.Provider());
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
    public async Task Handle_Should_ReturnNotFound_When_TheTokenCarriesNoNewEmail()
    {
        // Arrange: a token of another type never has a new email
        TokenInTheRepository(TokenData.PasswordReset(_user.Id));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnUsed_AndKeepTheEmail_When_TheTokenWasAlreadyUsed()
    {
        // Arrange
        TokenInTheRepository(TokenData.Used(TokenData.EmailChange(_user.Id, NewEmail)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Used);
        _user.Email.Should().Be(UserData.Email);
    }

    [Fact]
    public async Task Handle_Should_ReturnExpired_AndKeepTheEmail_When_TheTokenIsOlderThanOneHour()
    {
        // Arrange
        TokenInTheRepository(TokenData.EmailChange(_user.Id, NewEmail, TestClock.UtcNow.AddHours(-2)));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Expired);
        _user.Email.Should().Be(UserData.Email);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotUnique_AndLeaveTheTokenUnused_When_TheNewEmailWasTakenInTheMeantime()
    {
        // Arrange
        var token = TokenInTheRepository(TokenData.EmailChange(_user.Id, NewEmail));
        _users.EmailExistsAsync(NewEmail, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        token.Used.Should().BeFalse();
        _user.Email.Should().Be(UserData.Email);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_When_TheTokenOwnerNoLongerExists()
    {
        // Arrange
        TokenInTheRepository(TokenData.EmailChange(_user.Id, NewEmail));
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ChangeTheEmail_ConfirmIt_ConsumeTheToken_AndSave()
    {
        // Arrange
        var token = TokenInTheRepository(TokenData.EmailChange(_user.Id, NewEmail));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _user.Email.Should().Be(NewEmail);
        _user.IsEmailConfirmed.Should().BeTrue();
        token.Used.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
