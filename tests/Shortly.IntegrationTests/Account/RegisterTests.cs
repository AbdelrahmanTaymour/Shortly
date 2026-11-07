using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.Register;
using Shortly.Core.Exceptions;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Account;

public class RegisterTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private const string Password = "Str0ng-password!";

    private static RegisterCommand Register(string? email = null, string? username = null, string password = Password)
    {
        var unique = Guid.NewGuid().ToString("N");
        return new RegisterCommand(email ?? $"{unique}@example.com", username ?? $"user{unique}", password);
    }

    private Task<User> Stored(string email) => Query(db => db.Set<User>().SingleAsync(u => u.Email == email));

    private async Task<bool> VerifyPassword(string password, string hash)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify(password, hash);
    }

    // ---- success ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Register_Should_CreateAnUnconfirmedUser_WithAHashedPassword_AndIssueTokens()
    {
        // Arrange
        var command = Register();

        // Act
        var result = await Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var user = await Stored(command.Email);
        user.Username.Should().Be(command.Username);
        user.IsEmailConfirmed.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.PasswordHash.Should().NotBe(command.Password);
        (await VerifyPassword(command.Password, user.PasswordHash)).Should().BeTrue();

        var token = await Query(db => db.Set<RefreshToken>().SingleAsync(t => t.UserId == user.Id));
        token.IsActive(TestData.Now).Should().BeTrue();
    }

    // ---- duplicates ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Register_Should_RejectADuplicateEmail_RegardlessOfTheUsername()
    {
        // Arrange
        var existing = await Data.UserAsync();

        // Act
        var result = await Send(Register(email: existing.Email));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        (await Query(db => db.Set<User>().CountAsync(u => u.Email == existing.Email))).Should().Be(1);
    }

    [Fact]
    public async Task Register_Should_RejectADuplicateUsername_RegardlessOfTheEmail()
    {
        // Arrange
        var existing = await Data.UserAsync();

        // Act
        var result = await Send(Register(username: existing.Username));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        (await Query(db => db.Set<User>().CountAsync(u => u.Username == existing.Username))).Should().Be(1);
    }

    // ---- validation ---------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("not-an-email", "validusername", Password)]
    [InlineData("valid@example.com", "ab", Password)]
    [InlineData("valid@example.com", "validusername", "short")]
    public async Task Register_Should_RejectInvalidInput_BeforeStoringAnything(string email, string username, string password)
    {
        // Act
        var act = () => Send(new RegisterCommand(email, username, password));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        (await Query(db => db.Set<User>().AnyAsync(u => u.Email == email))).Should().BeFalse();
    }
}