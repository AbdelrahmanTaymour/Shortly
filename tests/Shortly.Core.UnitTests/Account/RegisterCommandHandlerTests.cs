using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.Register;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class RegisterCommandHandlerTests
{
    private static readonly RegisterCommand Command = new("jane@example.com", "jane", "Str0ng-password!");
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private static readonly AccessTokenResponse Tokens = new("access-token", "refresh-token", 900);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ICookieService _cookies = Substitute.For<ICookieService>();
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _passwordHasher.Hash(Command.Password).Returns("hashed-password");
        _jwtService.RefreshTokenLifetime.Returns(RefreshLifetime);
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(Tokens);
        _sut = new RegisterCommandHandler(_users, _passwordHasher, _jwtService, _cookies, TestClock.Provider());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotUnique_AndRegisterNobody_When_TheEmailOrUsernameIsTaken()
    {
        // Arrange
        _users.EmailOrUsernameExistsAsync(Command.Email, Command.Username, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
        await _jwtService.DidNotReceiveWithAnyArgs().IssueTokensAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_AddAnUnconfirmedUserWithTheHashedPassword()
    {
        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _users.Received(1).Add(Arg.Is<User>(u =>
            u.Email == Command.Email && u.Username == Command.Username
            && u.PasswordHash == "hashed-password" && !u.IsEmailConfirmed && u.Role == UserRole.User));
    }

    [Fact]
    public async Task Handle_Should_NeverStoreThePlainPassword()
    {
        // Act
        await _sut.Handle(Command, default);

        // Assert
        _users.DidNotReceive().Add(Arg.Is<User>(u => u.PasswordHash == Command.Password));
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
    public async Task Handle_Should_ReturnTheTokenError_AndNotSetTheCookie_When_TokenIssuingFails()
    {
        // Arrange
        var failure = Shortly.Domain.Abstractions.Error.Failure("Test.TokenFailure", "Could not issue tokens.");
        _jwtService.IssueTokensAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns(Shortly.Domain.Abstractions.Result.Failure<AccessTokenResponse>(failure));

        // Act
        var result = await _sut.Handle(Command, default);

        // Assert
        result.Error.Should().Be(failure);
        _cookies.DidNotReceiveWithAnyArgs().SetRefreshToken(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheDomainError_AndAddNobody_When_TheUserCannotBeCreated()
    {
        // Arrange: the Core validator normally prevents a blank email; the entity still guards it
        var command = Command with { Email = " " };

        // Act
        var result = await _sut.Handle(command, default);

        // Assert
        result.Error.Should().Be(UserErrors.EmailRequired);
        _users.DidNotReceiveWithAnyArgs().Add(default!);
    }
}
