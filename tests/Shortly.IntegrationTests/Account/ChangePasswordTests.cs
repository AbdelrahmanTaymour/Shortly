using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.ChangePassword;
using Shortly.Core.Exceptions;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Account;

public class ChangePasswordTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private const string CurrentPassword = "Current-password1!";
    private const string NewPassword = "Brand-new-password2!";

    private async Task<bool> VerifyPassword(string password, string hash)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify(password, hash);
    }

    private Task<string> StoredHash(Guid userId) => Query(db => db.Set<User>().Where(u => u.Id == userId).Select(u => u.PasswordHash).SingleAsync());

    private Task<List<RefreshToken>> TokensOf(Guid userId) => Query(db => db.Set<RefreshToken>().Where(t => t.UserId == userId).ToListAsync());

    // ---- success ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ChangePassword_Should_UpdateTheHash_AndRevokeEverySession_WhenTheCurrentPasswordIsCorrect()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(CurrentPassword);
        await Data.RefreshTokenAsync(user.Id);
        await Data.RefreshTokenAsync(user.Id);
        As(user.Id);

        // Act
        var result = await Send(new ChangePasswordCommand(CurrentPassword, NewPassword));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var hash = await StoredHash(user.Id);
        (await VerifyPassword(NewPassword, hash)).Should().BeTrue();
        (await VerifyPassword(CurrentPassword, hash)).Should().BeFalse();
        (await TokensOf(user.Id)).Should().OnlyContain(t => t.IsRevoked);
    }

    // ---- failure ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ChangePassword_Should_Fail_AndLeaveEverythingUnchanged_WhenTheCurrentPasswordIsWrong()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(CurrentPassword);
        var token = await Data.RefreshTokenAsync(user.Id);
        As(user.Id);

        // Act
        var result = await Send(new ChangePasswordCommand("totally-wrong-password", NewPassword));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
        (await VerifyPassword(CurrentPassword, await StoredHash(user.Id))).Should().BeTrue();
        (await Query(db => db.Set<RefreshToken>().Where(t => t.Id == token.Id).Select(t => t.IsRevoked).SingleAsync())).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Should_Fail_ForACallerThatNoLongerExists()
    {
        // Arrange: a caller that exists nowhere in the database
        As(Guid.NewGuid());

        // Act
        var result = await Send(new ChangePasswordCommand(CurrentPassword, NewPassword));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    // ---- validation ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ChangePassword_Should_RejectATooShortNewPassword_BeforeTouchingTheUser()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(CurrentPassword);
        As(user.Id);

        // Act
        var act = () => Send(new ChangePasswordCommand(CurrentPassword, "short"));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        (await VerifyPassword(CurrentPassword, await StoredHash(user.Id))).Should().BeTrue();
    }
}