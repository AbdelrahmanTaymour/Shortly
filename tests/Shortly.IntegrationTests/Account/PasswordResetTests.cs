using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortly.Core.Abstractions.Authentication;
using Shortly.Core.Account.ForgotPassword;
using Shortly.Core.Account.ResetPassword;
using Shortly.Core.Account.ValidateResetToken;
using Shortly.Domain.RefreshToken;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Account;

public class PasswordResetTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private const string OldPassword = "Old-password1!";
    private const string NewPassword = "New-password2!";

    private async Task<string> RequestResetTokenAsync(User user)
    {
        var result = await Send(new ForgotPasswordCommand(user.Email));
        result.IsSuccess.Should().BeTrue();
        return Emails.Sent.Last(e => e.Kind == "PasswordReset" && e.To == user.Email).Token;
    }

    private async Task<bool> VerifyPassword(string password, string hash)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify(password, hash);
    }

    private Task<string> StoredHash(Guid userId) => Query(db => db.Set<User>().Where(u => u.Id == userId).Select(u => u.PasswordHash).SingleAsync());

    private async Task<string> HashAsync(string rawToken)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return scope.ServiceProvider.GetRequiredService<ITokenHasher>().Hash(rawToken);
    }

    // ---- forgot password ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ForgotPassword_Should_IssueAUsableToken_AndEmailIt_ForAKnownAddress()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(OldPassword);

        // Act
        var result = await Send(new ForgotPasswordCommand(user.Email));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var sent = Emails.Sent.Should().ContainSingle(e => e.Kind == "PasswordReset" && e.To == user.Email).Which;
        var stored = await Query(db => db.Set<UserActionToken>().SingleAsync(t => t.UserId == user.Id));
        stored.TokenType.Should().Be(enUserActionTokenType.PasswordReset);
        stored.Used.Should().BeFalse();
        sent.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForgotPassword_Should_SucceedSilently_AndSendNoEmail_ForAnUnknownAddress()
    {
        // Act
        var result = await Send(new ForgotPasswordCommand("nobody@example.com"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Emails.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task ForgotPassword_Should_InvalidateThePreviousToken_WhenRequestedAgain()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(OldPassword);
        var firstToken = await RequestResetTokenAsync(user);

        // Act
        var secondToken = await RequestResetTokenAsync(user);

        // Assert: the first token can no longer be used, only the newest one can
        (await Send(new ValidateResetTokenCommand(firstToken))).Value.Should().BeFalse();
        (await Send(new ValidateResetTokenCommand(secondToken))).Value.Should().BeTrue();
    }

    // ---- validate reset token -------------------------------------------------------------------------------------------

    [Fact]
    public async Task ValidateResetToken_Should_ReturnFalse_ForATokenThatWasNeverIssued()
    {
        // Act
        var result = await Send(new ValidateResetTokenCommand("not-a-real-token"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    // ---- reset password ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ResetPassword_Should_ChangeThePassword_ConsumeTheToken_AndRevokeEverySession()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(OldPassword);
        await Data.RefreshTokenAsync(user.Id);
        var token = await RequestResetTokenAsync(user);

        // Act
        var result = await Send(new ResetPasswordCommand(token, NewPassword));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var hash = await StoredHash(user.Id);
        (await VerifyPassword(NewPassword, hash)).Should().BeTrue();
        (await VerifyPassword(OldPassword, hash)).Should().BeFalse();
        (await Query(db => db.Set<RefreshToken>().Where(t => t.UserId == user.Id).ToListAsync())).Should().OnlyContain(t => t.IsRevoked);
        (await Query(db => db.Set<UserActionToken>().Where(t => t.UserId == user.Id).Select(t => t.Used).SingleAsync())).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_Should_RejectAnAlreadyUsedToken()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(OldPassword);
        var token = await RequestResetTokenAsync(user);
        await Send(new ResetPasswordCommand(token, NewPassword));

        // Act
        var result = await Send(new ResetPasswordCommand(token, "Another-password3!"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.Used);
        (await VerifyPassword(NewPassword, await StoredHash(user.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_Should_RejectAnUnknownToken_WithoutChangingAnything()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(OldPassword);

        // Act
        var result = await Send(new ResetPasswordCommand("made-up-token", NewPassword));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.NotFound);
        (await VerifyPassword(OldPassword, await StoredHash(user.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_Should_RejectAnExpiredToken_WithoutChangingAnything()
    {
        // Arrange: a reset token is valid for one hour, so one issued two hours ago is already expired
        var user = await Data.UserWithPasswordAsync(OldPassword);
        const string rawToken = "a-known-raw-reset-token";
        await Data.PasswordResetTokenAsync(user.Id, tokenHash: await HashAsync(rawToken), issuedAt: TestData.Now.AddHours(-2));

        // Act
        var result = await Send(new ResetPasswordCommand(rawToken, NewPassword));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.Expired);
        (await VerifyPassword(OldPassword, await StoredHash(user.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_Should_RejectATooShortNewPassword_WithoutConsumingTheToken()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(OldPassword);
        var token = await RequestResetTokenAsync(user);

        // Act
        var act = () => Send(new ResetPasswordCommand(token, "short"));

        // Assert
        await act.Should().ThrowAsync<Shortly.Core.Exceptions.ValidationException>();
        (await Send(new ValidateResetTokenCommand(token))).Value.Should().BeTrue();
    }
}
