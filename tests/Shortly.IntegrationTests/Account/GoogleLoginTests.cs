using Microsoft.EntityFrameworkCore;
using Shortly.Core.Account.GoogleLogin;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Account;

public class GoogleLoginTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private static GoogleLoginCommand Login(string googleId, string email, string? name = "Jane Doe", string? picture = "https://example.com/pic.png") =>
        new(googleId, email, name, picture);

    private Task<User> ByEmail(string email) =>
        Query(db => db.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.Security)
            .Include(u => u.Usage)
            .SingleAsync(u => u.Email == email));
    
    // ---- brand-new account ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task GoogleLogin_Should_RegisterAConfirmedOAuthUser_AndIssueTokens_WhenNothingMatches()
    {
        // Arrange
        var email = $"{Guid.NewGuid():N}@example.com";
        var googleId = $"google-{Guid.NewGuid():N}";

        // Act
        var result = await Send(Login(googleId, email, "Jane Doe"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        var user = await ByEmail(email);
        user.GoogleId.Should().Be(googleId);
        user.IsOAuthUser.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeTrue();
        user.Profile.Name.Should().Be("Jane Doe");
        user.LastLoginAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task GoogleLogin_Should_SignInTheSameUser_OnASecondCall_WithoutDuplicating()
    {
        // Arrange
        var email = $"{Guid.NewGuid():N}@example.com";
        var googleId = $"google-{Guid.NewGuid():N}";
        await Send(Login(googleId, email));

        // Act
        var result = await Send(Login(googleId, email));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Query(db => db.Set<User>().CountAsync(u => u.Email == email))).Should().Be(1);
    }

    // ---- linking an existing account -------------------------------------------------------------------------------------

    [Fact]
    public async Task GoogleLogin_Should_LinkToAConfirmedExistingAccount_WithTheSameEmail()
    {
        // Arrange
        var existing = await Data.UserAsync(); // confirmed, no Google id yet
        var googleId = $"google-{Guid.NewGuid():N}";

        // Act
        var result = await Send(Login(googleId, existing.Email));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = await ByEmail(existing.Email);
        user.Id.Should().Be(existing.Id);
        user.GoogleId.Should().Be(googleId);
        (await Query(db => db.Set<User>().CountAsync(u => u.Email == existing.Email))).Should().Be(1);
    }

    [Fact]
    public async Task GoogleLogin_Should_RejectLinking_WhenTheExistingAccountsEmailIsUnconfirmed()
    {
        // Arrange: prevents an attacker from taking over an unverified mailbox via Google
        var existing = await Data.UserAsync(emailConfirmed: false);
        var googleId = $"google-{Guid.NewGuid():N}";

        // Act
        var result = await Send(Login(googleId, existing.Email));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.GoogleLinkRequiresConfirmedEmail);
        (await ByEmail(existing.Email)).GoogleId.Should().BeNull();
    }

    // ---- account state ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task GoogleLogin_Should_Fail_ForADeactivatedAccount_EvenWhenTheGoogleIdMatches()
    {
        // Arrange
        var email = $"{Guid.NewGuid():N}@example.com";
        var googleId = $"google-{Guid.NewGuid():N}";
        await Send(Login(googleId, email));
        await Seed(async db =>
        {
            var user = await db.Set<User>().SingleAsync(u => u.Email == email);
            user.Deactivate(TestData.Now);
        });

        // Act
        var result = await Send(Login(googleId, email));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.AccountDisabled);
    }
}
