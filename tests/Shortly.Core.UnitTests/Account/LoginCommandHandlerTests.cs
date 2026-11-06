using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.Login;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class LoginCommandHandlerTests
{
    private static readonly LoginCommand Command = new(UserData.Email, "Str0ng-password!");
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private static readonly AccessTokenResponse Tokens = new("access-token", "refresh-token", 900);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ICookieService _cookies = Substitute.For<ICookieService>();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _jwtService.RefreshTokenLifetime.Returns(RefreshLifetime);
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(Tokens);
        _sut = new LoginCommandHandler(_users, _passwordHasher, _jwtService, _cookies, TestClock.Provider());
    }

    private User ExistingUser(User user)
    {
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private void PasswordIs(bool correct) =>
        _passwordHasher.Verify(Command.Password, Arg.Any<string>()).Returns(correct);

    // ---- unknown email -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_When_NoUserHasThatEmail()
    {
        // Arrange
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidCredentials);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_StillVerifyAPassword_When_NoUserHasThatEmail_SoTheResponseTimeDoesNotRevealIt()
    {
        // Arrange
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await _sut.Handle(Command, default);

        // Assert
        _passwordHasher.Received(1).Verify(Command.Password, Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_NotRecordAFailedLogin_When_NoUserHasThatEmail()
    {
        // Arrange
        _users.GetByEmailAsync(Command.Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await _sut.Handle(Command, default);

        // Assert
        await _users.DidNotReceiveWithAnyArgs().RecordFailedLoginAsync(default, default, default, default);
    }

    // ---- wrong password ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnInvalidCredentials_When_ThePasswordIsWrong()
    {
        // Arrange
        ExistingUser(UserData.Create());
        PasswordIs(correct: false);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidCredentials);
        _cookies.DidNotReceiveWithAnyArgs().SetRefreshToken(default!, default);
    }

    [Fact]
    public async Task Handle_Should_RecordTheFailedLogin_WithTheFiveAttemptFifteenMinuteLockoutPolicy()
    {
        // Arrange
        var user = ExistingUser(UserData.Create());
        PasswordIs(correct: false);

        // Act
        await _sut.Handle(Command, default);

        // Assert
        await _users.Received(1).RecordFailedLoginAsync(
            user.Id, TestClock.UtcNow, 5, TimeSpan.FromMinutes(15), Arg.Any<CancellationToken>());
    }

    // ---- successful login --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnTheTokens_AndSetTheRefreshCookieForTheConfiguredLifetime()
    {
        // Arrange
        ExistingUser(UserData.Create());
        PasswordIs(correct: true);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().Be(Tokens);
        _cookies.Received(1).SetRefreshToken("refresh-token", TestClock.UtcNow.Add(RefreshLifetime));
    }

    [Fact]
    public async Task Handle_Should_StoreTheLoginTimeAndClearEarlierFailedAttempts()
    {
        // Arrange
        var user = ExistingUser(UserData.Create());
        user.RecordFailedLogin(TestClock.UtcNow, 5, TimeSpan.FromMinutes(15));
        PasswordIs(correct: true);

        // Act
        await _sut.Handle(Command, default);

        // Assert
        user.LastLoginAtUtc.Should().Be(TestClock.UtcNow);
        user.Security.FailedLoginAttempts.Should().Be(0);
        await _users.DidNotReceiveWithAnyArgs().RecordFailedLoginAsync(default, default, default, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheTokenError_AndNotSetTheCookie_When_TokenIssuingFails()
    {
        // Arrange
        ExistingUser(UserData.Create());
        PasswordIs(correct: true);
        var failure = Error.Failure("Test.TokenFailure", "Could not issue tokens.");
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AccessTokenResponse>(failure));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(failure);
        _cookies.DidNotReceiveWithAnyArgs().SetRefreshToken(default!, default);
    }

    // ---- account state -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnAccountLocked_WithoutTokens_When_ThePasswordIsRightButTheAccountIsLocked()
    {
        // Arrange
        ExistingUser(UserData.CreateLocked());
        PasswordIs(correct: true);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.AccountLocked);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
        _cookies.DidNotReceiveWithAnyArgs().SetRefreshToken(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnAccountDisabled_WithoutTokens_When_ThePasswordIsRightButTheAccountIsDeactivated()
    {
        // Arrange
        ExistingUser(UserData.CreateDeactivated());
        PasswordIs(correct: true);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.AccountDisabled);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_NotRevealThatTheAccountIsLocked_When_ThePasswordIsWrong()
    {
        // Arrange
        ExistingUser(UserData.CreateLocked());
        PasswordIs(correct: false);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidCredentials);
    }
}
