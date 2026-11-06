using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.GoogleLogin;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class GoogleLoginCommandHandlerTests
{
    private static readonly GoogleLoginCommand Command =
        new("google-123", UserData.Email, "Jane Doe", "https://example.com/pic.png");
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private static readonly AccessTokenResponse Tokens = new("access-token", "refresh-token", 900);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ICookieService _cookies = Substitute.For<ICookieService>();
    private readonly GoogleLoginCommandHandler _sut;

    public GoogleLoginCommandHandlerTests()
    {
        _jwtService.RefreshTokenLifetime.Returns(RefreshLifetime);
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(Tokens);
        _sut = new GoogleLoginCommandHandler(_users, _jwtService, _cookies, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_RegisterAConfirmedGoogleUserWithTheirName_When_NoAccountExists()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _users.Received(1).Add(Arg.Is<User>(u =>
            u.GoogleId == "google-123" && u.Email == UserData.Email && u.IsOAuthUser && u.IsEmailConfirmed
            && u.Profile.Name == "Jane Doe" && u.GoogleProfilePicture == "https://example.com/pic.png"));
    }

    [Fact]
    public async Task Handle_Should_SignInTheExistingGoogleUser_WithoutRegisteringAnotherOne()
    {
        // Arrange
        var existing = UserData.Create();
        _users.GetByGoogleIdAsync("google-123", Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().Be(Tokens);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
        await _jwtService.Received(1).IssueTokensAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTheTokens_AndSetTheRefreshCookieForTheConfiguredLifetime()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().Be(Tokens);
        _cookies.Received(1).SetRefreshToken("refresh-token", TestClock.UtcNow.Add(RefreshLifetime));
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndRegisterNobody_When_TheGoogleProfileHasNoEmail()
    {
        // Act
        var result = await _sut.Handle(Command with { Email = " " }, default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailRequired);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheTokenError_AndNotSetTheCookie_When_TokenIssuingFails()
    {
        // Arrange
        var failure = Error.Failure("Test.TokenFailure", "Could not issue tokens.");
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AccessTokenResponse>(failure));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(failure);
        _cookies.DidNotReceiveWithAnyArgs().SetRefreshToken(default!, default);
    }

    // ---- accounts that cannot sign in --------------------------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_ReturnAccountDisabled_WithoutTokens_When_TheGoogleUserIsDeactivated()
    {
        // Arrange
        _users.GetByGoogleIdAsync("google-123", Arg.Any<CancellationToken>()).Returns(UserData.CreateDeactivated());

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.AccountDisabled);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
        _cookies.DidNotReceiveWithAnyArgs().SetRefreshToken(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnAccountLocked_WithoutTokens_When_TheGoogleUserIsLocked()
    {
        // Arrange
        _users.GetByGoogleIdAsync("google-123", Arg.Any<CancellationToken>()).Returns(UserData.CreateLocked());

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.AccountLocked);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_StoreTheLoginTime_ForAnExistingGoogleUser()
    {
        // Arrange
        var existing = UserData.CreateGoogleUser();
        _users.GetByGoogleIdAsync("google-123", Arg.Any<CancellationToken>()).Returns(existing);

        // Act
        await _sut.Handle(Command, default);

        // Assert
        existing.LastLoginAtUtc.Should().Be(TestClock.UtcNow);
    }

    // ---- an account with the same email already exists ---------------------------------------------------------------

    [Fact]
    public async Task Handle_Should_RefuseTheSignIn_AndLinkNothing_When_AnUnconfirmedPasswordAccountHasTheSameEmail()
    {
        // Arrange: someone registered this address with a password but never proved they own it
        var passwordAccount = UserData.Create();
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(passwordAccount);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.GoogleLinkRequiresConfirmedEmail);
        passwordAccount.GoogleId.Should().BeNull();
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Handle_Should_LinkTheGoogleIdentity_AndSignIn_When_AConfirmedPasswordAccountHasTheSameEmail()
    {
        // Arrange
        var passwordAccount = UserData.CreateConfirmed();
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(passwordAccount);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Value.Should().Be(Tokens);
        passwordAccount.GoogleId.Should().Be("google-123");
        passwordAccount.GoogleProfilePicture.Should().Be("https://example.com/pic.png");
        passwordAccount.IsOAuthUser.Should().BeFalse();
        _users.DidNotReceiveWithAnyArgs().Add(default!);
        await _jwtService.Received(1).IssueTokensAsync(passwordAccount, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnAlreadyLinked_When_TheAccountWithThatEmailBelongsToADifferentGoogleAccount()
    {
        // Arrange
        var other = UserData.CreateGoogleUser(googleId: "someone-elses-google-id");
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(other);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.GoogleAccountAlreadyLinked);
        other.GoogleId.Should().Be("someone-elses-google-id");
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnAccountDisabled_When_TheMatchingAccountIsDeactivated()
    {
        // Arrange
        var deactivated = UserData.CreateConfirmed();
        deactivated.Deactivate(TestClock.UtcNow);
        _users.GetByEmailAsync(UserData.Email, Arg.Any<CancellationToken>()).Returns(deactivated);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.AccountDisabled);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
    }
}
