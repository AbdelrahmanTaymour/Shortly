using Shortly.Domain.Tokens;
using Shortly.Domain.UnitTests.TestData;

namespace Shortly.Domain.UnitTests.Tokens;

public class UserActionTokenTests
{
    private static readonly DateTime Now = TestClock.Now;

    // ---- issuing -----------------------------------------------------------------------------------------------------

    [Fact]
    public void IssueEmailVerification_Should_ExpireAfter24Hours()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = UserActionToken.IssueEmailVerification(userId, "hash", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var token = result.Value;
        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(userId);
        token.TokenType.Should().Be(enUserActionTokenType.EmailVerification);
        token.TokenHash.Should().Be("hash");
        token.ExpiresAt.Should().Be(Now.AddHours(24));
        token.CreatedAt.Should().Be(Now);
        token.NewEmail.Should().BeNull();
        token.Used.Should().BeFalse();
        token.UsedAt.Should().BeNull();
    }

    [Fact]
    public void IssuePasswordReset_Should_ExpireAfterOneHour()
    {
        // Act
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;

        // Assert
        token.TokenType.Should().Be(enUserActionTokenType.PasswordReset);
        token.ExpiresAt.Should().Be(Now.AddHours(1));
        token.NewEmail.Should().BeNull();
    }

    [Fact]
    public void IssueEmailChange_Should_RememberTheNewEmail_AndExpireAfterOneHour()
    {
        // Act
        var result = UserActionToken.IssueEmailChange(Guid.NewGuid(), "hash", "new@example.com", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TokenType.Should().Be(enUserActionTokenType.EmailChange);
        result.Value.NewEmail.Should().Be("new@example.com");
        result.Value.ExpiresAt.Should().Be(Now.AddHours(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IssueEmailChange_Should_Fail_When_TheNewEmailIsMissing(string? newEmail)
    {
        // Act
        var result = UserActionToken.IssueEmailChange(Guid.NewGuid(), "hash", newEmail!, Now);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.NewEmailRequired);
    }

    [Fact]
    public void Issue_Should_GenerateADifferentIdForEachToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var first = UserActionToken.IssuePasswordReset(userId, "hash", Now).Value;
        var second = UserActionToken.IssuePasswordReset(userId, "hash", Now).Value;

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_Should_Fail_When_TheTokenHashIsBlank(string? tokenHash)
    {
        // Act
        var verification = UserActionToken.IssueEmailVerification(Guid.NewGuid(), tokenHash!, Now);
        var reset = UserActionToken.IssuePasswordReset(Guid.NewGuid(), tokenHash!, Now);
        var change = UserActionToken.IssueEmailChange(Guid.NewGuid(), tokenHash!, "new@example.com", Now);

        // Assert
        verification.Error.Should().Be(UserActionTokenErrors.TokenHashRequired);
        reset.Error.Should().Be(UserActionTokenErrors.TokenHashRequired);
        change.Error.Should().Be(UserActionTokenErrors.TokenHashRequired);
    }

    [Fact]
    public void Issue_Should_Fail_When_TheUserIsAnEmptyGuid()
    {
        // Act
        var verification = UserActionToken.IssueEmailVerification(Guid.Empty, "hash", Now);
        var reset = UserActionToken.IssuePasswordReset(Guid.Empty, "hash", Now);
        var change = UserActionToken.IssueEmailChange(Guid.Empty, "hash", "new@example.com", Now);

        // Assert
        verification.Error.Should().Be(UserActionTokenErrors.UserRequired);
        reset.Error.Should().Be(UserActionTokenErrors.UserRequired);
        change.Error.Should().Be(UserActionTokenErrors.UserRequired);
    }

    // ---- validation --------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(enUserActionTokenType.EmailVerification)]
    [InlineData(enUserActionTokenType.PasswordReset)]
    public void EnsureValidFor_Should_Succeed_ForAnUnusedUnexpiredTokenOfTheExpectedType(enUserActionTokenType type)
    {
        // Arrange
        var token = IssueOfType(type);

        // Act
        var result = token.EnsureValidFor(type, Now.AddMinutes(30));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EnsureValidFor_Should_Succeed_ForAnEmailChangeToken()
    {
        // Arrange
        var token = UserActionToken.IssueEmailChange(Guid.NewGuid(), "hash", "new@example.com", Now).Value;

        // Act
        var result = token.EnsureValidFor(enUserActionTokenType.EmailChange, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EnsureValidFor_Should_Fail_When_TheTypeDiffers()
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;

        // Act
        var result = token.EnsureValidFor(enUserActionTokenType.EmailVerification, Now);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.WrongType);
    }

    [Fact]
    public void EnsureValidFor_Should_Fail_When_TheTokenWasUsed()
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;
        token.MarkUsed(Now);

        // Act
        var result = token.EnsureValidFor(enUserActionTokenType.PasswordReset, Now);

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Used);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void EnsureValidFor_Should_TreatTheExactExpiryInstantAsExpired(int secondsRelativeToExpiry, bool valid)
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;

        // Act
        var result = token.EnsureValidFor(enUserActionTokenType.PasswordReset, token.ExpiresAt.AddSeconds(secondsRelativeToExpiry));

        // Assert
        result.IsSuccess.Should().Be(valid);
        if (!valid) result.Error.Should().Be(UserActionTokenErrors.Expired);
    }

    [Fact]
    public void EnsureValidFor_Should_ReportTheTypeError_Before_UsedAndExpired()
    {
        // Arrange: wrong type, used and expired at once
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;
        token.MarkUsed(Now);

        // Act
        var result = token.EnsureValidFor(enUserActionTokenType.EmailVerification, Now.AddDays(5));

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.WrongType);
    }

    [Fact]
    public void EnsureValidFor_Should_ReportUsed_BeforeExpired()
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;
        token.MarkUsed(Now);

        // Act
        var result = token.EnsureValidFor(enUserActionTokenType.PasswordReset, Now.AddDays(5));

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Used);
    }

    [Fact]
    public void EnsureValidFor_Should_NotChangeTheToken()
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;

        // Act
        token.EnsureValidFor(enUserActionTokenType.PasswordReset, Now);

        // Assert
        token.Used.Should().BeFalse();
        token.UsedAt.Should().BeNull();
    }

    // ---- consuming ---------------------------------------------------------------------------------------------------

    [Fact]
    public void MarkUsed_Should_FlagTheTokenAndStoreTheTime()
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;
        var later = Now.AddMinutes(10);

        // Act
        token.MarkUsed(later);

        // Assert
        token.Used.Should().BeTrue();
        token.UsedAt.Should().Be(later);
    }

    [Fact]
    public void MarkUsed_Should_Succeed_ForAnUnusedToken()
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;

        // Act
        var result = token.MarkUsed(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void MarkUsed_Should_Fail_AndKeepTheFirstUseTime_WhenCalledAgain()
    {
        // Arrange
        var token = UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value;
        token.MarkUsed(Now);

        // Act
        var result = token.MarkUsed(Now.AddMinutes(5));

        // Assert
        result.Error.Should().Be(UserActionTokenErrors.Used);
        token.UsedAt.Should().Be(Now);
    }

    private static UserActionToken IssueOfType(enUserActionTokenType type) => type switch
    {
        enUserActionTokenType.EmailVerification => UserActionToken.IssueEmailVerification(Guid.NewGuid(), "hash", Now).Value,
        _ => UserActionToken.IssuePasswordReset(Guid.NewGuid(), "hash", Now).Value
    };
}
