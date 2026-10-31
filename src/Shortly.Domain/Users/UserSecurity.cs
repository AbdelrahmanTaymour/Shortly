using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Users;

public sealed class UserSecurity
{
    private UserSecurity(Guid userId) => UserId = userId;
    private UserSecurity() { }

    public Guid UserId { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public string? LockoutReason { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public string? TwoFactorSecret { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    internal static UserSecurity CreateDefault(Guid userId) => new(userId);

    public void RecordFailedAttempt(DateTime utcNow, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        UpdatedAtUtc = utcNow;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedUntilUtc = utcNow.Add(lockoutDuration);
            LockoutReason = $"{maxAttempts} consecutive failed login attempts";
        }
    }

    public void ResetFailedLoginAttempts(DateTime utcNow)
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        LockoutReason = null;
        UpdatedAtUtc = utcNow;
    }

    public bool IsLockedOut(DateTime utcNow) => LockedUntilUtc is not null && LockedUntilUtc > utcNow;

    public Result LockManually(string reason, TimeSpan duration, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(UserErrors.LockReasonRequired);

        LockedUntilUtc = utcNow.Add(duration);
        LockoutReason = reason;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }
    
    public Result EnableTwoFactor(string secret, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(secret)) return Result.Failure(UserErrors.TwoFactorSecretRequired);
        TwoFactorEnabled = true;
        TwoFactorSecret = secret;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result DisableTwoFactor(DateTime utcNow)
    {
        TwoFactorEnabled = false;
        TwoFactorSecret = null;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }
}