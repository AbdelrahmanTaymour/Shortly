using Shortly.Domain.Users;

namespace Shortly.Domain.UnitTests.TestData;

internal static class UserData
{
    public const string Email = "jane@example.com";
    public const string Username = "jane";
    public const string PasswordHash = "hashed-password";

    public static User CreateUser(
        string email = Email,
        string username = Username,
        string passwordHash = PasswordHash,
        DateTime? utcNow = null) =>
        User.Register(email, username, passwordHash, utcNow ?? TestClock.Now).Value;

    public static User CreateOAuthUser(string email = Email, string googleId = "google-123") =>
        User.RegisterOAuth(email, googleId, "https://example.com/pic.png", TestClock.Now).Value;

    /// <summary>Registers a user and locks the account through failed logins, the way production does.</summary>
    public static User CreateLockedUser(int maxAttempts = 3, TimeSpan? lockoutDuration = null)
    {
        var user = CreateUser();
        for (var i = 0; i < maxAttempts; i++)
            user.RecordFailedLogin(TestClock.Now, maxAttempts, lockoutDuration ?? TimeSpan.FromMinutes(15));
        return user;
    }
}
