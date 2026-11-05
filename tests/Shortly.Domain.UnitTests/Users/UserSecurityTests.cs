using Shortly.Domain.UnitTests.TestData;
using Shortly.Domain.Users;

namespace Shortly.Domain.UnitTests.Users;

public class UserSecurityTests
{
    private static readonly DateTime Now = TestClock.Now;
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);

    private static UserSecurity NewSecurity() => UserData.CreateUser().Security;

    [Fact]
    public void NewSecurity_Should_StartUnlockedWithoutFailedAttemptsOrTwoFactor()
    {
        // Act
        var security = NewSecurity();

        // Assert
        security.FailedLoginAttempts.Should().Be(0);
        security.LockedUntilUtc.Should().BeNull();
        security.LockoutReason.Should().BeNull();
        security.TwoFactorEnabled.Should().BeFalse();
        security.TwoFactorSecret.Should().BeNull();
        security.IsLockedOut(Now).Should().BeFalse();
    }

    // ---- failed attempts ---------------------------------------------------------------------------------------------

    [Fact]
    public void RecordFailedAttempt_Should_CountWithoutLocking_BelowTheMaximum()
    {
        // Arrange
        var security = NewSecurity();

        // Act
        security.RecordFailedAttempt(Now, maxAttempts: 3, Lockout);
        security.RecordFailedAttempt(Now, maxAttempts: 3, Lockout);

        // Assert
        security.FailedLoginAttempts.Should().Be(2);
        security.LockedUntilUtc.Should().BeNull();
        security.IsLockedOut(Now).Should().BeFalse();
    }

    [Fact]
    public void RecordFailedAttempt_Should_LockTheAccount_WhenTheMaximumIsReached()
    {
        // Arrange
        var security = NewSecurity();

        // Act
        for (var i = 0; i < 3; i++) security.RecordFailedAttempt(Now, maxAttempts: 3, Lockout);

        // Assert
        security.FailedLoginAttempts.Should().Be(3);
        security.LockedUntilUtc.Should().Be(Now.Add(Lockout));
        security.LockoutReason.Should().Be("3 consecutive failed login attempts");
        security.IsLockedOut(Now).Should().BeTrue();
        security.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void IsLockedOut_Should_BeFalse_AtTheExactInstantTheLockoutEnds()
    {
        // Arrange
        var security = NewSecurity();
        for (var i = 0; i < 3; i++) security.RecordFailedAttempt(Now, maxAttempts: 3, Lockout);
        var lockedUntil = Now.Add(Lockout);

        // Assert
        security.IsLockedOut(lockedUntil.AddTicks(-1)).Should().BeTrue();
        security.IsLockedOut(lockedUntil).Should().BeFalse();
    }

    [Fact]
    public void RecordFailedAttempt_Should_IgnoreAttempts_WhileTheAccountIsLocked()
    {
        // Arrange
        var security = NewSecurity();
        for (var i = 0; i < 3; i++) security.RecordFailedAttempt(Now, maxAttempts: 3, Lockout);
        var lockedUntil = security.LockedUntilUtc;
        var during = Now.AddMinutes(5);

        // Act
        security.RecordFailedAttempt(during, maxAttempts: 3, Lockout);

        // Assert: neither the counter nor the lockout window moved
        security.FailedLoginAttempts.Should().Be(3);
        security.LockedUntilUtc.Should().Be(lockedUntil);
        security.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void RecordFailedAttempt_Should_StartCountingAgain_OnceTheLockoutHasExpired()
    {
        // Arrange
        var security = NewSecurity();
        for (var i = 0; i < 3; i++) security.RecordFailedAttempt(Now, maxAttempts: 3, Lockout);
        var afterLockout = Now.Add(Lockout).AddMinutes(1);

        // Act
        security.RecordFailedAttempt(afterLockout, maxAttempts: 3, Lockout);

        // Assert: a clean slate, not an instant re-lock
        security.FailedLoginAttempts.Should().Be(1);
        security.LockedUntilUtc.Should().BeNull();
        security.LockoutReason.Should().BeNull();
        security.IsLockedOut(afterLockout).Should().BeFalse();
    }

    [Fact]
    public void RecordFailedAttempt_Should_LockAgain_AfterAnotherFullRoundOfFailures_FollowingAnExpiredLockout()
    {
        // Arrange
        var security = NewSecurity();
        for (var i = 0; i < 3; i++) security.RecordFailedAttempt(Now, maxAttempts: 3, Lockout);
        var afterLockout = Now.Add(Lockout).AddMinutes(1);

        // Act
        for (var i = 0; i < 3; i++) security.RecordFailedAttempt(afterLockout, maxAttempts: 3, Lockout);

        // Assert
        security.FailedLoginAttempts.Should().Be(3);
        security.LockedUntilUtc.Should().Be(afterLockout.Add(Lockout));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void RecordFailedAttempt_Should_Throw_When_MaxAttemptsIsLessThanOne(int maxAttempts)
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var act = () => security.RecordFailedAttempt(Now, maxAttempts, Lockout);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxAttempts");
        security.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void ResetFailedLoginAttempts_Should_ClearCounterAndLockout()
    {
        // Arrange
        var security = UserData.CreateLockedUser().Security;
        var later = Now.AddMinutes(1);

        // Act
        security.ResetFailedLoginAttempts(later);

        // Assert
        security.FailedLoginAttempts.Should().Be(0);
        security.LockedUntilUtc.Should().BeNull();
        security.LockoutReason.Should().BeNull();
        security.UpdatedAtUtc.Should().Be(later);
        security.IsLockedOut(later).Should().BeFalse();
    }

    // ---- manual lock -------------------------------------------------------------------------------------------------

    [Fact]
    public void LockManually_Should_LockForTheGivenDurationWithTheReason()
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var result = security.LockManually("Suspicious activity", TimeSpan.FromDays(1), Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        security.LockedUntilUtc.Should().Be(Now.AddDays(1));
        security.LockoutReason.Should().Be("Suspicious activity");
        security.IsLockedOut(Now).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LockManually_Should_Fail_When_ReasonIsMissing(string? reason)
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var result = security.LockManually(reason!, TimeSpan.FromDays(1), Now);

        // Assert
        result.Error.Should().Be(UserErrors.LockReasonRequired);
        security.LockedUntilUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void LockManually_Should_Fail_AndNotLock_When_DurationIsNotPositive(int seconds)
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var result = security.LockManually("Reason", TimeSpan.FromSeconds(seconds), Now);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidLockDuration);
        security.IsLockedOut(Now).Should().BeFalse();
        security.LockoutReason.Should().BeNull();
    }

    [Fact]
    public void LockManually_Should_ReportTheReasonError_First_When_BothInputsAreInvalid()
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var result = security.LockManually(" ", TimeSpan.Zero, Now);

        // Assert
        result.Error.Should().Be(UserErrors.LockReasonRequired);
    }

    // ---- two-factor --------------------------------------------------------------------------------------------------

    [Fact]
    public void EnableTwoFactor_Should_StoreTheSecretAndEnableIt()
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var result = security.EnableTwoFactor("secret", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        security.TwoFactorEnabled.Should().BeTrue();
        security.TwoFactorSecret.Should().Be("secret");
        security.UpdatedAtUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void EnableTwoFactor_Should_Fail_When_SecretIsMissing(string? secret)
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var result = security.EnableTwoFactor(secret!, Now);

        // Assert
        result.Error.Should().Be(UserErrors.TwoFactorSecretRequired);
        security.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public void DisableTwoFactor_Should_ClearTheSecretAndDisableIt()
    {
        // Arrange
        var security = NewSecurity();
        security.EnableTwoFactor("secret", Now);

        // Act
        var result = security.DisableTwoFactor(Now.AddMinutes(1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        security.TwoFactorEnabled.Should().BeFalse();
        security.TwoFactorSecret.Should().BeNull();
    }

    [Fact]
    public void DisableTwoFactor_Should_Succeed_When_ItWasNeverEnabled()
    {
        // Arrange
        var security = NewSecurity();

        // Act
        var result = security.DisableTwoFactor(Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
