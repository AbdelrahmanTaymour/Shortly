using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.LogoutAll;
using Shortly.Core.UnitTests.TestData;

namespace Shortly.Core.UnitTests.Account;

public class LogoutAllCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_RevokeEveryRefreshTokenOfTheCaller_AndDeleteTheCookie()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jwtService = Substitute.For<IJwtService>();
        var cookies = Substitute.For<ICookieService>();
        var sut = new LogoutAllCommandHandler(CallerData.Create(userId), jwtService, cookies);

        // Act
        var result = await sut.Handle(new LogoutAllCommand(), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await jwtService.Received(1).RevokeAllForUserAsync(userId, Arg.Any<CancellationToken>());
        cookies.Received(1).DeleteRefreshToken();
    }
}
