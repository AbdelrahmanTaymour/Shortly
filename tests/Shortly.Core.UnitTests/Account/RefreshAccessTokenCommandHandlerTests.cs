using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.RefreshAccessToken;
using Shortly.Core.UnitTests.TestData;
using Shortly.Domain.Abstractions;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.Account;

public class RefreshAccessTokenCommandHandlerTests
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private static readonly AccessTokenResponse NewTokens = new("new-access-token", "new-refresh-token", 900);

    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ICookieService _cookies = Substitute.For<ICookieService>();
    private readonly RefreshAccessTokenCommandHandler _sut;

    public RefreshAccessTokenCommandHandlerTests()
    {
        _jwtService.RefreshTokenLifetime.Returns(RefreshLifetime);
        _sut = new RefreshAccessTokenCommandHandler(_jwtService, _cookies, TestClock.Provider());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_Should_ReturnInvalidRefreshToken_WithoutCallingTheJwtService_When_ThereIsNoRefreshCookie(string? cookie)
    {
        // Arrange
        _cookies.GetRefreshToken().Returns(cookie);

        // Act
        var result = await _sut.Handle(new RefreshAccessTokenCommand(), default);

        // Assert
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        await _jwtService.DidNotReceiveWithAnyArgs().RefreshAccessTokenAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheJwtServiceError_AndLeaveTheCookieAlone_When_TheRefreshIsRejected()
    {
        // Arrange
        _cookies.GetRefreshToken().Returns("old-refresh-token");
        _jwtService.RefreshAccessTokenAsync("old-refresh-token", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AccessTokenResponse>(AuthenticationErrors.InvalidRefreshToken));

        // Act
        var result = await _sut.Handle(new RefreshAccessTokenCommand(), default);

        // Assert
        result.Error.Should().Be(AuthenticationErrors.InvalidRefreshToken);
        _cookies.DidNotReceiveWithAnyArgs().SetRefreshToken(default!, default);
    }

    [Fact]
    public async Task Handle_Should_ReturnTheNewTokens_AndRotateTheCookieForTheConfiguredLifetime()
    {
        // Arrange
        _cookies.GetRefreshToken().Returns("old-refresh-token");
        _jwtService.RefreshAccessTokenAsync("old-refresh-token", Arg.Any<CancellationToken>()).Returns(NewTokens);

        // Act
        var result = await _sut.Handle(new RefreshAccessTokenCommand(), default);

        // Assert
        result.Value.Should().Be(NewTokens);
        _cookies.Received(1).SetRefreshToken("new-refresh-token", TestClock.UtcNow.Add(RefreshLifetime));
    }
}
