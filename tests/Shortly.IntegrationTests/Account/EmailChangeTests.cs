using Microsoft.EntityFrameworkCore;
using Shortly.Core.Account.ConfirmEmailChange;
using Shortly.Core.Account.ForgotPassword;
using Shortly.Core.Account.InitiateEmailChange;
using Shortly.Core.Exceptions;
using Shortly.Domain.Tokens;
using Shortly.Domain.Users;
using Shortly.IntegrationTests.Infrastructure;

namespace Shortly.IntegrationTests.Account;

public class EmailChangeTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private const string Password = "Current-password1!";

    private async Task<string> RequestEmailChangeAsync(string newEmail)
    {
        var result = await Send(new InitiateEmailChangeCommand(newEmail, Password));
        result.IsSuccess.Should().BeTrue();
        return Emails.Sent.Last(e => e.Kind == "EmailChange" && e.To == newEmail).Token;
    }

    private Task<User> Stored(Guid id) => Query(db => db.Set<User>().SingleAsync(u => u.Id == id));

    // ---- initiate -------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task InitiateEmailChange_Should_IssueAUsableToken_AndEmailItToTheNewAddress()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(Password);
        As(user.Id);
        var newEmail = $"{Guid.NewGuid():N}@example.com";

        // Act
        var result = await Send(new InitiateEmailChangeCommand(newEmail, Password));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Emails.Sent.Should().ContainSingle(e => e.Kind == "EmailChange" && e.To == newEmail);
        var token = await Query(db => db.Set<UserActionToken>().SingleAsync(t => t.UserId == user.Id));
        token.TokenType.Should().Be(enUserActionTokenType.EmailChange);
        token.NewEmail.Should().Be(newEmail);
        (await Stored(user.Id)).Email.Should().Be(user.Email); // not changed until confirmed
    }

    [Fact]
    public async Task InitiateEmailChange_Should_Fail_WhenTheCurrentPasswordIsWrong()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(Password);
        As(user.Id);

        // Act
        var result = await Send(new InitiateEmailChangeCommand("new@example.com", "wrong-password"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidCredentials);
        Emails.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiateEmailChange_Should_RejectAnEmailAlreadyInUse()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(Password);
        var other = await Data.UserAsync();
        As(user.Id);

        // Act
        var result = await Send(new InitiateEmailChangeCommand(other.Email, Password));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        Emails.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiateEmailChange_Should_RejectAMalformedEmail_BeforeTouchingAnything()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(Password);
        As(user.Id);

        // Act
        var act = () => Send(new InitiateEmailChangeCommand("not-an-email", Password));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        Emails.Sent.Should().BeEmpty();
    }

    // ---- confirm --------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ConfirmEmailChange_Should_SwapTheAddress_AndConsumeTheToken()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(Password);
        As(user.Id);
        var newEmail = $"{Guid.NewGuid():N}@example.com";
        var token = await RequestEmailChangeAsync(newEmail);

        // Act
        var result = await Send(new ConfirmEmailChangeCommand(token));

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await Stored(user.Id)).Email.Should().Be(newEmail);
        (await Query(db => db.Set<UserActionToken>().Where(t => t.UserId == user.Id).Select(t => t.Used).SingleAsync())).Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmEmailChange_Should_RejectAnAlreadyUsedToken()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(Password);
        As(user.Id);
        var token = await RequestEmailChangeAsync($"{Guid.NewGuid():N}@example.com");
        await Send(new ConfirmEmailChangeCommand(token));

        // Act
        var result = await Send(new ConfirmEmailChangeCommand(token));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.Used);
    }

    [Fact]
    public async Task ConfirmEmailChange_Should_RejectAnUnknownToken()
    {
        // Act
        var result = await Send(new ConfirmEmailChangeCommand("made-up-token"));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.NotFound);
    }

    [Fact]
    public async Task ConfirmEmailChange_Should_RejectAPasswordResetToken_IssuedToTheSameUser()
    {
        // Arrange: a token issued for a different purpose carries no NewEmail and must not be accepted here
        var user = await Data.UserWithPasswordAsync(Password);
        (await Send(new ForgotPasswordCommand(user.Email))).IsSuccess.Should().BeTrue();
        var resetToken = Emails.Sent.Last(e => e.Kind == "PasswordReset" && e.To == user.Email).Token;

        // Act
        var result = await Send(new ConfirmEmailChangeCommand(resetToken));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserActionTokenErrors.NotFound);
    }

    [Fact]
    public async Task ConfirmEmailChange_Should_FailTheEmailUniquenessCheckAgain_IfTheAddressWasTakenAfterTheTokenWasIssued()
    {
        // Arrange
        var user = await Data.UserWithPasswordAsync(Password);
        As(user.Id);
        var newEmail = $"{Guid.NewGuid():N}@example.com";
        var token = await RequestEmailChangeAsync(newEmail);

        // Someone else claims the address in the meantime
        await Data.UserAsync(email: newEmail);

        // Act
        var result = await Send(new ConfirmEmailChangeCommand(token));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailOrUsernameNotUnique);
        (await Stored(user.Id)).Email.Should().NotBe(newEmail);
    }
}
