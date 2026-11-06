using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Account.ForgotPassword;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class ForgotPasswordCommandHandlerTests
{
    private static readonly ForgotPasswordCommand Command = new(UserData.Email);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEmailNotifier _notifier = Substitute.For<IEmailNotifier>();
    private readonly ForgotPasswordCommandHandler _sut;

    public ForgotPasswordCommandHandlerTests()
    {
        _tokenHasher.GenerateToken().Returns("plain-token");
        _tokenHasher.Hash("plain-token").Returns("hashed-token");
        _sut = new ForgotPasswordCommandHandler(_users, _tokens, _tokenHasher, _unitOfWork, _notifier, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_SucceedWithoutDoingAnything_When_NoUserHasThatEmail()
    {
        // Arrange: the response must not reveal whether an account exists
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokens.DidNotReceiveWithAnyArgs().Add(default!);
        _notifier.DidNotReceiveWithAnyArgs().EnqueuePasswordResetAsync(default!, default!, default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_StoreOnlyTheHashOfAOneHourPasswordResetToken()
    {
        // Arrange
        var user = UserData.Create();
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokens.Received(1).Add(Arg.Is<UserActionToken>(t =>
            t.UserId == user.Id && t.TokenType == enUserActionTokenType.PasswordReset
            && t.TokenHash == "hashed-token" && t.ExpiresAt == TestClock.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task Handle_Should_InvalidateEarlierResetTokens_BeforeIssuingTheNewOne()
    {
        // Arrange
        var user = UserData.Create();
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        await _sut.Handle(Command, default);

        // Assert
        Received.InOrder(() =>
        {
            _tokens.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.PasswordReset, TestClock.UtcNow, Arg.Any<CancellationToken>());
            _tokens.Add(Arg.Any<UserActionToken>());
        });
    }

    [Fact]
    public async Task Handle_Should_EmailThePlainToken_OnlyAfterTheHashWasSaved()
    {
        // Arrange
        var user = UserData.Create();
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        await _sut.Handle(Command, default);

        // Assert
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _notifier.EnqueuePasswordResetAsync(user.Email, user.Username, "plain-token");
        });
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSendNothing_When_TheTokenCannotBeIssued()
    {
        // Arrange
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns(UserData.Create());
        _tokenHasher.Hash("plain-token").Returns(string.Empty);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.TokenHashRequired);
        _notifier.DidNotReceiveWithAnyArgs().EnqueuePasswordResetAsync(default!, default!, default!);
    }
}
