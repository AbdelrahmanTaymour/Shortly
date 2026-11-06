using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Account.InitiateEmailChange;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class InitiateEmailChangeCommandHandlerTests
{
    private static readonly InitiateEmailChangeCommand Command = new("new@example.com", "current-password");

    private readonly User _user = UserData.Create();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEmailNotifier _notifier = Substitute.For<IEmailNotifier>();
    private readonly InitiateEmailChangeCommandHandler _sut;

    public InitiateEmailChangeCommandHandlerTests()
    {
        _users.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _passwordHasher.Verify(Command.CurrentPassword, UserData.PasswordHash).Returns(true);
        _tokenHasher.GenerateToken().Returns("plain-token");
        _tokenHasher.Hash("plain-token").Returns("hashed-token");
        _sut = new InitiateEmailChangeCommandHandler(
            CallerData.Create(_user.Id), _users, _tokens, _passwordHasher, _tokenHasher, _unitOfWork,
            TestClock.Provider(), _notifier);
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
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_AndIssueNothing_When_TheCurrentPasswordIsWrong()
    {
        // Arrange
        _passwordHasher.Verify(Command.CurrentPassword, UserData.PasswordHash).Returns(false);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidCredentials);
        _tokens.DidNotReceiveWithAnyArgs().Add(default!);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueEmailChangeConfirmationAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotUnique_AndIssueNothing_When_TheNewEmailIsAlreadyInUse()
    {
        // Arrange
        _users.EmailExistsAsync(Command.NewEmail, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        _tokens.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_IssueAOneHourTokenRememberingTheNewEmail_AndLeaveTheCurrentEmailUntouched()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokens.Received(1).Add(Arg.Is<UserActionToken>(t =>
            t.UserId == _user.Id && t.TokenType == enUserActionTokenType.EmailChange && t.NewEmail == Command.NewEmail
            && t.TokenHash == "hashed-token" && t.ExpiresAt == TestClock.UtcNow.AddHours(1)));
        _user.Email.Should().Be(UserData.Email);
    }

    [Fact]
    public async Task Handle_Should_InvalidateEarlierTokens_ThenSave_ThenEmailThePlainTokenToTheNewAddress()
    {
        // Act
        await _sut.Handle(Command, default);

        // Assert
        Received.InOrder(() =>
        {
            _tokens.InvalidateActiveTokensAsync(_user.Id, enUserActionTokenType.EmailChange, TestClock.UtcNow, Arg.Any<CancellationToken>());
            _tokens.Add(Arg.Any<UserActionToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _notifier.EnqueueEmailChangeConfirmationAsync(Command.NewEmail, _user.Username, "plain-token");
        });
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSendNothing_When_TheTokenCannotBeIssued()
    {
        // Arrange
        _tokenHasher.Hash("plain-token").Returns(string.Empty);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.TokenHashRequired);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueEmailChangeConfirmationAsync(default!, default!, default!);
    }
}
