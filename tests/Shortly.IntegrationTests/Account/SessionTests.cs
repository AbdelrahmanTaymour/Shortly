using Microsoft.EntityFrameworkCore;
using Shortly.Core.Account.Logout;
using Shortly.Core.Account.LogoutAll;
using Shortly.Domain.RefreshToken;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Account;

public class SessionTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private Task<List<RefreshToken>> TokensOf(Guid userId) => Query(db => db.Set<RefreshToken>().Where(t => t.UserId == userId).ToListAsync());

    // ---- logout ---------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Logout_Should_Succeed_EvenWithNoRefreshCookiePresent()
    {
        // Arrange
        var user = await Data.UserAsync();
        await Data.RefreshTokenAsync(user.Id);
        As(user.Id);

        // Act
        var result = await Send(new LogoutCommand());

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await TokensOf(user.Id)).Should().OnlyContain(t => !t.IsRevoked);
    }

    // ---- logout-all -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task LogoutAll_Should_RevokeEveryActiveSession_ForTheCaller()
    {
        // Arrange
        var user = await Data.UserAsync();
        await Data.RefreshTokenAsync(user.Id);
        await Data.RefreshTokenAsync(user.Id);
        await Data.RefreshTokenAsync(user.Id);
        As(user.Id);

        // Act
        var result = await Send(new LogoutAllCommand());

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await TokensOf(user.Id)).Should().OnlyContain(t => t.IsRevoked);
    }

    [Fact]
    public async Task LogoutAll_Should_NotTouchAnotherUsersSessions()
    {
        // Arrange
        var caller = await Data.UserAsync();
        var bystander = await Data.UserAsync();
        await Data.RefreshTokenAsync(caller.Id);
        var bystanderToken = await Data.RefreshTokenAsync(bystander.Id);
        As(caller.Id);

        // Act
        await Send(new LogoutAllCommand());

        // Assert
        (await Query(db => db.Set<RefreshToken>().Where(t => t.Id == bystanderToken.Id).Select(t => t.IsRevoked).SingleAsync()))
            .Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAll_Should_Succeed_ForACallerWithNoActiveSessions()
    {
        // Arrange
        var user = await Data.UserAsync();
        As(user.Id);

        // Act
        var result = await Send(new LogoutAllCommand());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}