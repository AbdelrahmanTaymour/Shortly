using Microsoft.EntityFrameworkCore;
using Shortly.Core.Account.ForgotPassword;
using Shortly.Core.Account.SendEmailVerification;
using Shortly.Core.Account.VerifyEmail;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Account;

public class EmailVerificationTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private async Task<User> UnconfirmedUserAsync() => await Data.UserAsync(emailConfirmed: false);

    private async Task<string> RequestVerificationTokenAsync(string email)
    {
        var result = await Send(new SendEmailVerificationCommand(email));
        result.IsSuccess.Should().BeTrue();
        return Emails.Sent.Last(e => e.Kind == "EmailVerification" && e.To == email).Token;
    }

    // ---- send verification --------------------------------------------------------------------------------------------

    [Fact]
    public async Task SendEmailVerification_Should_IssueAUsableToken_AndEmailIt_ForAnUnconfirmedAddress()
    {
        // Arrange
        var user = await UnconfirmedUserAsync();

        // Act
        var result = await Send(new SendEmailVerificationCommand(user.Email));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Emails.Sent.Should().ContainSingle(e => e.Kind == "EmailVerification" && e.To == user.Email);
        (await Query(db => db.Set<UserActionToken>().SingleAsync(t => t.UserId == user.Id))).TokenType
            .Should().Be(enUserActionTokenType.EmailVerification);
    }

    [Fact]
    public async Task SendEmailVerification_Should_UseTheCallersOwnEmail_WhenNoneIsGiven()
    {
        // Arrange
        var user = await UnconfirmedUserAsync();
        As(Caller.User(user.Id, email: user.Email));

        // Act
        var result = await Send(new SendEmailVerificationCommand(null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Emails.Sent.Should().ContainSingle(e => e.Kind == "EmailVerification" && e.To == user.Email);
    }

    [Fact]
    public async Task SendEmailVerification_Should_SucceedSilently_AndSendNoEmail_ForAnAlreadyConfirmedAddress()
    {
        // Arrange: Data.UserAsync() confirms the email by default
        var user = await Data.UserAsync();

        // Act
        var result = await Send(new SendEmailVerificationCommand(user.Email));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Emails.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task SendEmailVerification_Should_SucceedSilently_AndSendNoEmail_ForAnUnknownAddress()
    {
        // Act
        var result = await Send(new SendEmailVerificationCommand("nobody@example.com"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Emails.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task SendEmailVerification_Should_InvalidateThePreviousToken_WhenRequestedAgain()
    {
        // Arrange
        var user = await UnconfirmedUserAsync();
        var firstToken = await RequestVerificationTokenAsync(user.Email);

        // Act
        var secondToken = await RequestVerificationTokenAsync(user.Email);

        // Assert: only the newest token can confirm the address
        (await Send(new VerifyEmailCommand(secondToken))).IsSuccess.Should().BeTrue();
        var reconfirm = await Send(new VerifyEmailCommand(firstToken));
        reconfirm.IsFailure.Should().BeTrue();
        reconfirm.Error.Should().Be(UserActionTokenErrors.Used);
    }

    // ---- verify email -------------------------------------------------------------------------------------------------

    [Fact]
    public async Task VerifyEmail_Should_ConfirmTheAddress_ConsumeTheToken_AndIssueTokens()
    {
        // Arrange
        var user = await UnconfirmedUserAsync();
        var token = await RequestVerificationTokenAsync(user.Email);

        // Act
        var result = await Send(new VerifyEmailCommand(token));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        (await Query(db => db.Set<User>().Where(u => u.Id == user.Id).Select(u => u.IsEmailConfirmed).SingleAsync())).Should().BeTrue();
        (await Query(db => db.Set<UserActionToken>().Where(t => t.UserId == user.Id).Select(t => t.Used).SingleAsync())).Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmail_Should_RejectAnAlreadyUsedToken()
    {
        // Arrange
        var user = await UnconfirmedUserAsync();
        var token = await RequestVerificationTokenAsync(user.Email);
        await Send(new VerifyEmailCommand(token));

        // Act
        var result = await Send(new VerifyEmailCommand(token));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.Used);
    }

    [Fact]
    public async Task VerifyEmail_Should_RejectAnUnknownToken()
    {
        // Act
        var result = await Send(new VerifyEmailCommand("made-up-token"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.NotFound);
    }

    [Fact]
    public async Task VerifyEmail_Should_RejectAPasswordResetToken_IssuedToTheSameUser()
    {
        // Arrange: a token issued for a different purpose must not confirm the email
        var user = await UnconfirmedUserAsync();
        var resetToken = await Send(new ForgotPasswordCommand(user.Email));
        resetToken.IsSuccess.Should().BeTrue();
        var token = Emails.Sent.Last(e => e.Kind == "PasswordReset" && e.To == user.Email).Token;

        // Act
        var result = await Send(new VerifyEmailCommand(token));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.WrongType);
    }
}
