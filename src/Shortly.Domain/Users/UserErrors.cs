using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Users;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "User not found.");
    public static readonly Error InvalidCredentials = Error.Unauthorized("User.InvalidCredentials", "Invalid email or password.");
    public static readonly Error AccountLocked = Error.Unauthorized("User.AccountLocked", "This account is temporarily locked due to too many failed login attempts.");
    public static readonly Error EmailOrUsernameNotUnique = Error.Conflict("User.EmailOrUsernameNotUnique", "This email or username is already in use.");
    public static readonly Error EmailRequired = Error.Validation("User.EmailRequired", "Email is required.");
    public static readonly Error UsernameRequired = Error.Validation("User.UsernameRequired", "Username is required.");
    public static readonly Error PasswordRequired = Error.Validation("User.PasswordRequired", "Password is required.");
    public static readonly Error EmailAlreadyConfirmed = Error.Conflict("User.EmailAlreadyConfirmed", "Email is already confirmed.");
    public static readonly Error AlreadyActive = Error.Conflict("User.AlreadyActive", "User is already active.");
    public static readonly Error AlreadyInactive = Error.Conflict("User.AlreadyInactive", "User is already inactive.");
    public static readonly Error AlreadyDeleted = Error.Conflict("User.AlreadyDeleted", "User is already deleted.");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized("User.InvalidRefreshToken", "Refresh token is missing or invalid.");
    public static readonly Error TwoFactorSecretRequired = Error.Validation("User.TwoFactorSecretRequired", "A two-factor secret is required.");
    public static readonly Error LockReasonRequired = Error.Validation("User.LockReasonRequired", "A lock reason is required.");
}