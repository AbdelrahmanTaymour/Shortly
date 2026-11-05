using Shortly.Domain.SubscriptionPlan;
using Shortly.Domain.UnitTests.TestData;
using Shortly.Domain.Users;

namespace Shortly.Domain.UnitTests.Users;

public class UserTests
{
    private static readonly DateTime Now = TestClock.Now;
    private static readonly DateTime Later = TestClock.Now.AddHours(1);
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);

    // ---- registration ------------------------------------------------------------------------------------------------

    [Fact]
    public void Register_Should_CreateAnActiveFreeUser_WithUnconfirmedEmail()
    {
        // Act
        var result = User.Register("jane@example.com", "jane", "hash", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = result.Value;
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("jane@example.com");
        user.Username.Should().Be("jane");
        user.PasswordHash.Should().Be("hash");
        user.Role.Should().Be(UserRole.User);
        user.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Free);
        user.IsActive.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeFalse();
        user.IsOAuthUser.Should().BeFalse();
        user.IsDeleted.Should().BeFalse();
        user.LastLoginAtUtc.Should().BeNull();
        user.CreatedAtUtc.Should().Be(Now);
        user.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Register_Should_CreateDefaultProfileSecurityAndUsage_ForTheUser()
    {
        // Act
        var user = UserData.CreateUser();

        // Assert
        user.Profile.UserId.Should().Be(user.Id);
        user.Security.UserId.Should().Be(user.Id);
        user.Security.FailedLoginAttempts.Should().Be(0);
        user.Usage.UserId.Should().Be(user.Id);
        user.Usage.MonthlyLinksCreated.Should().Be(0);
        user.Usage.MonthlyResetDate.Should().Be(Now.AddMonths(1));
    }

    [Fact]
    public void Register_Should_GenerateADifferentIdForEachUser()
    {
        // Act
        var first = UserData.CreateUser();
        var second = UserData.CreateUser();

        // Assert
        first.Id.Should().NotBe(second.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Should_Fail_When_EmailIsMissing(string? email)
    {
        // Act
        var result = User.Register(email!, "jane", "hash", Now);

        // Assert
        result.Error.Should().Be(UserErrors.EmailRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Should_Fail_When_UsernameIsMissing(string? username)
    {
        // Act
        var result = User.Register("jane@example.com", username!, "hash", Now);

        // Assert
        result.Error.Should().Be(UserErrors.UsernameRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Should_Fail_When_PasswordHashIsMissing(string? passwordHash)
    {
        // Act
        var result = User.Register("jane@example.com", "jane", passwordHash!, Now);

        // Assert
        result.Error.Should().Be(UserErrors.PasswordRequired);
    }

    [Fact]
    public void Register_Should_ReportTheEmailError_First_When_EverythingIsMissing()
    {
        // Act
        var result = User.Register("", "", "", Now);

        // Assert
        result.Error.Should().Be(UserErrors.EmailRequired);
    }

    [Theory]
    [InlineData("not-an-email", "has spaces in it")]
    [InlineData("@@", "x")]
    public void Register_Should_OnlyRequireNonBlankValues_BecauseFormatRulesBelongToTheCoreValidators(string email, string username)
    {
        // Act: email format and username length are enforced by RegisterCommandValidator before the entity is reached
        var result = User.Register(email, username, "hash", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RegisterOAuth_Should_CreateAConfirmedGoogleUser_WithEmailAsUsername()
    {
        // Act
        var result = User.RegisterOAuth("jane@example.com", "google-123", "https://example.com/pic.png", Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = result.Value;
        user.IsOAuthUser.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeTrue();
        user.GoogleId.Should().Be("google-123");
        user.GoogleProfilePicture.Should().Be("https://example.com/pic.png");
        user.Email.Should().Be("jane@example.com");
        user.Username.Should().Be("jane@example.com");
        user.PasswordHash.Should().BeEmpty();
        user.Role.Should().Be(UserRole.User);
        user.SubscriptionPlanId.Should().Be(enSubscriptionPlan.Free);
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterOAuth_Should_Fail_When_EmailIsMissing(string? email)
    {
        // Act
        var result = User.RegisterOAuth(email!, "google-123", null, Now);

        // Assert
        result.Error.Should().Be(UserErrors.EmailRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterOAuth_Should_Fail_When_GoogleIdIsMissing(string? googleId)
    {
        // Act
        var result = User.RegisterOAuth("jane@example.com", googleId!, null, Now);

        // Assert
        result.Error.Should().Be(UserErrors.GoogleIdRequired);
    }

    [Fact]
    public void RegisterOAuth_Should_Succeed_WithoutAProfilePicture()
    {
        // Act
        var result = User.RegisterOAuth("jane@example.com", "google-123", null, Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GoogleProfilePicture.Should().BeNull();
    }

    // ---- credentials and profile data ---------------------------------------------------------------------------------

    [Fact]
    public void ChangePassword_Should_ReplaceTheHashAndTouchTheTimestamp()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangePassword("new-hash", Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("new-hash");
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void ChangePassword_Should_ClearFailedAttemptsAndLockout()
    {
        // Arrange
        var user = UserData.CreateLockedUser();
        user.CanLogIn(Now).Should().BeFalse();

        // Act
        user.ChangePassword("new-hash", Later);

        // Assert
        user.Security.FailedLoginAttempts.Should().Be(0);
        user.Security.LockedUntilUtc.Should().BeNull();
        user.CanLogIn(Later).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ChangePassword_Should_Fail_AndKeepTheOldHash_When_HashIsMissing(string? hash)
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangePassword(hash!, Later);

        // Assert
        result.Error.Should().Be(UserErrors.PasswordRequired);
        user.PasswordHash.Should().Be(UserData.PasswordHash);
        user.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void ChangeEmail_Should_ReplaceTheEmail_AndMarkItConfirmed()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.IsEmailConfirmed.Should().BeFalse();

        // Act
        var result = user.ChangeEmail("new@example.com", Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Email.Should().Be("new@example.com");
        user.IsEmailConfirmed.Should().BeTrue();
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ChangeEmail_Should_Fail_AndKeepTheOldEmail_When_EmailIsMissing(string? email)
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangeEmail(email!, Later);

        // Assert
        result.Error.Should().Be(UserErrors.EmailRequired);
        user.Email.Should().Be(UserData.Email);
        user.IsEmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public void ChangeUsername_Should_ReplaceTheUsernameAndTouchTheTimestamp()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangeUsername("janet", Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Username.Should().Be("janet");
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ChangeUsername_Should_Fail_AndKeepTheOldUsername_When_UsernameIsMissing(string? username)
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangeUsername(username!, Later);

        // Assert
        result.Error.Should().Be(UserErrors.UsernameRequired);
        user.Username.Should().Be(UserData.Username);
    }

    [Fact]
    public void ConfirmEmail_Should_ConfirmTheEmail()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ConfirmEmail(Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsEmailConfirmed.Should().BeTrue();
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void ConfirmEmail_Should_Fail_When_AlreadyConfirmed()
    {
        // Arrange
        var user = UserData.CreateOAuthUser();

        // Act
        var result = user.ConfirmEmail(Later);

        // Assert
        result.Error.Should().Be(UserErrors.EmailAlreadyConfirmed);
        user.UpdatedAtUtc.Should().Be(Now);
    }

    // ---- login -------------------------------------------------------------------------------------------------------

    [Fact]
    public void CanLogIn_Should_BeTrue_ForAnActiveUnlockedUser()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Assert
        user.CanLogIn(Now).Should().BeTrue();
    }

    [Fact]
    public void CanLogIn_Should_BeFalse_When_Deactivated()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.Deactivate(Later);

        // Assert
        user.CanLogIn(Later).Should().BeFalse();
    }

    [Fact]
    public void CanLogIn_Should_BeFalse_When_Deleted()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.SoftDelete(Guid.NewGuid(), Later);

        // Assert
        user.CanLogIn(Later).Should().BeFalse();
    }

    [Fact]
    public void CanLogIn_Should_BeFalse_WhileLocked_AndTrueOnceTheLockoutHasPassed()
    {
        // Arrange
        var user = UserData.CreateLockedUser(maxAttempts: 3, lockoutDuration: Lockout);
        var lockedUntil = Now.Add(Lockout);

        // Assert
        user.CanLogIn(lockedUntil.AddSeconds(-1)).Should().BeFalse();
        user.CanLogIn(lockedUntil).Should().BeTrue();
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_StoreTheLoginTimeAndClearFailedAttempts()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.RecordFailedLogin(Now, maxAttempts: 5, Lockout);
        user.RecordFailedLogin(Now, maxAttempts: 5, Lockout);

        // Act
        var result = user.RecordSuccessfulLogin(Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.LastLoginAtUtc.Should().Be(Later);
        user.Security.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_Fail_When_TheAccountIsLocked()
    {
        // Arrange
        var user = UserData.CreateLockedUser();

        // Act
        var result = user.RecordSuccessfulLogin(Now);

        // Assert
        result.Error.Should().Be(UserErrors.AccountLocked);
        user.LastLoginAtUtc.Should().BeNull();
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_ReportAccountDisabled_ForADeactivatedUser()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.Deactivate(Now);

        // Act
        var result = user.RecordSuccessfulLogin(Later);

        // Assert
        result.Error.Should().Be(UserErrors.AccountDisabled);
        user.LastLoginAtUtc.Should().BeNull();
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_ReportAccountDisabled_ForADeletedUser()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.SoftDelete(Guid.NewGuid(), Now);

        // Act
        var result = user.RecordSuccessfulLogin(Later);

        // Assert
        result.Error.Should().Be(UserErrors.AccountDisabled);
    }

    [Fact]
    public void RecordSuccessfulLogin_Should_ReportDisabled_BeforeLocked_When_BothApply()
    {
        // Arrange
        var user = UserData.CreateLockedUser();
        user.Deactivate(Now);

        // Act
        var result = user.RecordSuccessfulLogin(Now);

        // Assert
        result.Error.Should().Be(UserErrors.AccountDisabled);
    }

    [Fact]
    public void RecordFailedLogin_Should_LockTheAccount_WhenTheMaximumIsReached()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        user.RecordFailedLogin(Now, maxAttempts: 2, Lockout);
        user.CanLogIn(Now).Should().BeTrue();
        user.RecordFailedLogin(Now, maxAttempts: 2, Lockout);

        // Assert
        user.CanLogIn(Now).Should().BeFalse();
        user.Security.LockedUntilUtc.Should().Be(Now.Add(Lockout));
    }

    // ---- activation and deletion ---------------------------------------------------------------------------------------

    [Fact]
    public void Deactivate_Should_MakeTheUserInactive()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.Deactivate(Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void Deactivate_Should_Fail_When_AlreadyInactive()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.Deactivate(Now);

        // Act
        var result = user.Deactivate(Later);

        // Assert
        result.Error.Should().Be(UserErrors.AlreadyInactive);
        user.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Activate_Should_MakeTheUserActive()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.Deactivate(Now);

        // Act
        var result = user.Activate(Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void Activate_Should_Fail_When_AlreadyActive()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.Activate(Later);

        // Assert
        result.Error.Should().Be(UserErrors.AlreadyActive);
    }

    [Fact]
    public void SoftDelete_Should_FlagTheUserAsDeletedAndInactive()
    {
        // Arrange
        var user = UserData.CreateUser();
        var admin = Guid.NewGuid();

        // Act
        var result = user.SoftDelete(admin, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        user.DeletedAtUtc.Should().Be(Later);
        user.DeletedBy.Should().Be(admin);
        user.IsActive.Should().BeFalse();
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Fact]
    public void SoftDelete_Should_Fail_When_AlreadyDeleted()
    {
        // Arrange
        var user = UserData.CreateUser();
        var firstAdmin = Guid.NewGuid();
        user.SoftDelete(firstAdmin, Now);

        // Act
        var result = user.SoftDelete(Guid.NewGuid(), Later);

        // Assert
        result.Error.Should().Be(UserErrors.AlreadyDeleted);
        user.DeletedBy.Should().Be(firstAdmin);
        user.DeletedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Activate_Should_Fail_When_TheUserIsDeleted()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.SoftDelete(Guid.NewGuid(), Now);

        // Act
        var result = user.Activate(Later);

        // Assert
        result.Error.Should().Be(UserErrors.Deleted);
        user.IsActive.Should().BeFalse();
        user.UpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Deactivate_Should_Fail_When_TheUserIsDeleted()
    {
        // Arrange
        var user = UserData.CreateUser();
        user.SoftDelete(Guid.NewGuid(), Now);

        // Act
        var result = user.Deactivate(Later);

        // Assert
        result.Error.Should().Be(UserErrors.Deleted);
        user.UpdatedAtUtc.Should().Be(Now);
    }

    // ---- role and plan -------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(UserRole.SuperAdmin)]
    [InlineData(UserRole.SystemAdmin)]
    [InlineData(UserRole.User)]
    public void ChangeRole_Should_SetTheRoleAndTouchTheTimestamp(UserRole role)
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangeRole(role, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Role.Should().Be(role);
        user.UpdatedAtUtc.Should().Be(Later);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void ChangeRole_Should_Fail_AndKeepTheRole_When_RoleIsNotDefined(int value)
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangeRole((UserRole)value, Later);

        // Assert
        result.Error.Should().Be(UserErrors.InvalidRole);
        user.Role.Should().Be(UserRole.User);
        user.UpdatedAtUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData(enSubscriptionPlan.Starter)]
    [InlineData(enSubscriptionPlan.Professional)]
    [InlineData(enSubscriptionPlan.Enterprise)]
    [InlineData(enSubscriptionPlan.Free)]
    public void ChangeSubscriptionPlan_Should_SetThePlanAndTouchTheTimestamp(enSubscriptionPlan plan)
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        var result = user.ChangeSubscriptionPlan(plan, Later);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.SubscriptionPlanId.Should().Be(plan);
        user.UpdatedAtUtc.Should().Be(Later);
    }
}
