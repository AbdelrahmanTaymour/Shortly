using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Abstractions.Email;
using Shortly.Core.Account.SendEmailVerification;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class SendEmailVerificationCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUserActionTokenRepository _tokens = Substitute.For<IUserActionTokenRepository>();
    private readonly IUserContext _caller = Substitute.For<IUserContext>();
    private readonly ITokenHasher _tokenHasher = Substitute.For<ITokenHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEmailNotifier _notifier = Substitute.For<IEmailNotifier>();
    private readonly SendEmailVerificationCommandHandler _sut;

    public SendEmailVerificationCommandHandlerTests()
    {
        _caller.Email.Returns("caller@example.com");
        _tokenHasher.GenerateToken().Returns("plain-token");
        _tokenHasher.Hash("plain-token").Returns("hashed-token");
        _sut = new SendEmailVerificationCommandHandler(
            _users, _tokens, _caller, _tokenHasher, _unitOfWork, TestClock.Provider(), _notifier);
    }

    [Fact]
    public async Task Handle_Should_SucceedWithoutDoingAnything_When_NoUserHasThatEmail()
    {
        // Arrange
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(new SendEmailVerificationCommand("nobody@example.com"), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokens.DidNotReceiveWithAnyArgs().Add(default!);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueSendEmailVerificationAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Handle_Should_LookUpTheEmailOfTheRequest_When_ItIsProvided()
    {
        // Arrange
        var user = UserData.Create();
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        await _sut.Handle(new SendEmailVerificationCommand(UserData.Email), default);

        // Assert
        await _users.Received(1).GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_FallBackToTheCallersEmail_When_TheRequestHasNone()
    {
        // Act
        await _sut.Handle(new SendEmailVerificationCommand(null), default);

        // Assert
        await _users.Received(1).GetByEmailAsync("caller@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_IssueA24HourVerificationToken_InvalidateEarlierOnes_AndEmailThePlainTokenAfterSaving()
    {
        // Arrange
        var user = UserData.Create();
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _sut.Handle(new SendEmailVerificationCommand(UserData.Email), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Received.InOrder(() =>
        {
            _tokens.InvalidateActiveTokensAsync(user.Id, enUserActionTokenType.EmailVerification, TestClock.UtcNow, Arg.Any<CancellationToken>());
            _tokens.Add(Arg.Is<UserActionToken>(t =>
                t.UserId == user.Id && t.TokenType == enUserActionTokenType.EmailVerification
                && t.TokenHash == "hashed-token" && t.ExpiresAt == TestClock.UtcNow.AddHours(24)));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _notifier.EnqueueSendEmailVerificationAsync(user.Email, user.Username, "plain-token");
        });
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndSendNothing_When_TheTokenCannotBeIssued()
    {
        // Arrange
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(UserData.Create());
        _tokenHasher.Hash("plain-token").Returns(string.Empty);

        // Act
        var result = await _sut.Handle(new SendEmailVerificationCommand(UserData.Email), default);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.TokenHashRequired);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueSendEmailVerificationAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Handle_Should_SucceedSilently_WithoutSendingAnything_When_TheEmailIsAlreadyConfirmed()
    {
        // Arrange: same response as for an unknown address, so the endpoint reveals nothing about the account
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(UserData.CreateConfirmed());

        // Act
        var result = await _sut.Handle(new SendEmailVerificationCommand(UserData.Email), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _tokens.DidNotReceiveWithAnyArgs().Add(default!);
        _notifier.DidNotReceiveWithAnyArgs().EnqueueSendEmailVerificationAsync(default!, default!, default!);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
