using Shortly.Domain.Tokens;
using Shortly.Domain.Users;

namespace Shortly.Core.UnitTests.TestData;

internal static class UserData
{
    public const string Email = "jane@example.com";
    public const string PasswordHash = "stored-hash";

    public static User Create(string email = Email, string username = "jane", string passwordHash = PasswordHash) =>
        User.Register(email, username, passwordHash, TestClock.UtcNow).Value;

    /// <summary>A user whose id is the given one, for tests where the caller and the target are the same account.</summary>
    public static User CreateWithId(Guid id, string email = "admin@example.com", string username = "admin")
    {
        var user = Create(email, username);
        typeof(Shortly.Domain.Abstractions.Entity<Guid>).GetProperty(nameof(user.Id))!.SetValue(user, id);
        return user;
    }

    public static User CreateGoogleUser(string email = Email, string googleId = "google-123") =>
        User.RegisterOAuth(email, googleId, null, TestClock.UtcNow).Value;

    public static User CreateConfirmed()
    {
        var user = Create();
        user.ConfirmEmail(TestClock.UtcNow);
        return user;
    }

    public static User CreateLocked()
    {
        var user = Create();
        for (var i = 0; i < 3; i++) user.RecordFailedLogin(TestClock.UtcNow, 3, TimeSpan.FromMinutes(15));
        return user;
    }

    public static User CreateDeactivated()
    {
        var user = Create();
        user.Deactivate(TestClock.UtcNow);
        return user;
    }
}

internal static class TokenData
{
    public const string Hash = "token-hash";

    public static UserActionToken EmailVerification(Guid userId, DateTime? issuedAt = null) =>
        UserActionToken.IssueEmailVerification(userId, Hash, issuedAt ?? TestClock.UtcNow).Value;

    public static UserActionToken PasswordReset(Guid userId, DateTime? issuedAt = null) =>
        UserActionToken.IssuePasswordReset(userId, Hash, issuedAt ?? TestClock.UtcNow).Value;

    public static UserActionToken EmailChange(Guid userId, string newEmail = "new@example.com", DateTime? issuedAt = null) =>
        UserActionToken.IssueEmailChange(userId, Hash, newEmail, issuedAt ?? TestClock.UtcNow).Value;

    public static UserActionToken Used(UserActionToken token)
    {
        token.MarkUsed(TestClock.UtcNow);
        return token;
    }
}
