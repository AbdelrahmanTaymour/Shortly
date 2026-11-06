using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.Logout;

namespace Shortly.Core.UnitTests.Account;

public class LogoutCommandHandlerTests
{
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ICookieService _cookies = Substitute.For<ICookieService>();
    private readonly LogoutCommandHandler _sut;

    public LogoutCommandHandlerTests() => _sut = new LogoutCommandHandler(_jwtService, _cookies);

    [Fact]
    public async Task Handle_Should_RevokeTheRefreshToken_AndDeleteTheCookie()
    {
        // Arrange
        _cookies.GetRefreshToken().Returns("refresh-token");

        // Act
        var result = await _sut.Handle(new LogoutCommand(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _jwtService.Received(1).RevokeAsync("refresh-token", Arg.Any<CancellationToken>());
        _cookies.Received(1).DeleteRefreshToken();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_Should_StillSucceedAndDeleteTheCookie_WithoutRevoking_When_ThereIsNoRefreshCookie(string? cookie)
    {
        // Arrange
        _cookies.GetRefreshToken().Returns(cookie);

        // Act
        var result = await _sut.Handle(new LogoutCommand(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _jwtService.DidNotReceiveWithAnyArgs().RevokeAsync(default!, default);
        _cookies.Received(1).DeleteRefreshToken();
    }
}
