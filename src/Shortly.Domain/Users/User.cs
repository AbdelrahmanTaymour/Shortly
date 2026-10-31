using Shortly.Domain.Abstractions;
using Shortly.Domain.Enums;

namespace Shortly.Domain.Users;

public sealed class User : Entity<Guid>
{
    private User(
        Guid id, string email, string username, string passwordHash, UserRole role,
        bool isOAuthUser, string? googleId, string? googleProfilePicture, bool isEmailConfirmed, DateTime utcNow)
        : base(id)
    {
        Email = email;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
        SubscriptionPlanId = enSubscriptionPlan.Free;
        IsActive = true;
        IsEmailConfirmed = isEmailConfirmed;
        IsOAuthUser = isOAuthUser;
        GoogleId = googleId;
        GoogleProfilePicture = googleProfilePicture;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        Profile = UserProfile.CreateDefault(id);
        Security = UserSecurity.CreateDefault(id);
        Usage = UserUsage.CreateDefault(id, utcNow);
    }

    private User() { }

    public string Email { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public enSubscriptionPlan SubscriptionPlanId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public Guid? DeletedBy { get; private set; }
    public string? GoogleId { get; private set; }
    public string? GoogleProfilePicture { get; private set; }
    public bool IsOAuthUser { get; private set; }

    public UserProfile Profile { get; private set; } = null!;
    public UserSecurity Security { get; private set; } = null!;
    public UserUsage Usage { get; private set; } = null!;

    public static Result<User> Register(string email, string username, string passwordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(email)) return Result.Failure<User>(UserErrors.EmailRequired);
        if (string.IsNullOrWhiteSpace(username)) return Result.Failure<User>(UserErrors.UsernameRequired);
        if (string.IsNullOrWhiteSpace(passwordHash)) return Result.Failure<User>(UserErrors.PasswordRequired);

        return new User(Guid.NewGuid(), email, username, passwordHash, UserRole.User,
            isOAuthUser: false, googleId: null, googleProfilePicture: null, isEmailConfirmed: false, utcNow);
    }

    public static User RegisterOAuth(string email, string googleId, string? googleProfilePicture, DateTime utcNow) =>
        new(Guid.NewGuid(), email, email, passwordHash: string.Empty, UserRole.User,
            isOAuthUser: true, googleId, googleProfilePicture, isEmailConfirmed: true, utcNow);

    public Result ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash)) return Result.Failure(UserErrors.PasswordRequired);
        PasswordHash = newPasswordHash;
        UpdatedAtUtc = utcNow;
        Security.ResetFailedLoginAttempts(utcNow);
        return Result.Success();
    }

    public Result ChangeEmail(string newEmail, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newEmail)) return Result.Failure(UserErrors.EmailRequired);
        Email = newEmail;
        IsEmailConfirmed = true; // reaching this point means the change was already verified via token
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result ChangeUsername(string newUsername, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(newUsername)) return Result.Failure(UserErrors.UsernameRequired);
        Username = newUsername;
        UpdatedAtUtc = utcNow;
        return Result.Success();   
    }

    public Result ConfirmEmail(DateTime utcNow)
    {
        if (IsEmailConfirmed) return Result.Failure(UserErrors.EmailAlreadyConfirmed);
        IsEmailConfirmed = true;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result RecordSuccessfulLogin(DateTime utcNow)
    {
        if (!CanLogIn(utcNow)) return Result.Failure(UserErrors.AccountLocked);

        LastLoginAtUtc = utcNow;
        Security.ResetFailedLoginAttempts(utcNow);
        return Result.Success();
    }

    public void RecordFailedLogin(DateTime utcNow, int maxAttempts, TimeSpan lockoutDuration) =>
        Security.RecordFailedAttempt(utcNow, maxAttempts, lockoutDuration);

    public Result Activate(DateTime utcNow)
    {
        if (IsActive) return Result.Failure(UserErrors.AlreadyActive);
        IsActive = true;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result Deactivate(DateTime utcNow)
    {
        if (!IsActive) return Result.Failure(UserErrors.AlreadyInactive);
        IsActive = false;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result SoftDelete(Guid deletedBy, DateTime utcNow)
    {
        if (IsDeleted) return Result.Failure(UserErrors.AlreadyDeleted);
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedBy = deletedBy;
        IsActive = false;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result ChangeRole(UserRole role, DateTime utcNow)
    {
        Role = role;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result ChangeSubscriptionPlan(enSubscriptionPlan planId, DateTime utcNow)
    {
        SubscriptionPlanId = planId;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }
    
    public bool CanLogIn(DateTime utcNow) => IsActive && !IsDeleted && !Security.IsLockedOut(utcNow);
}