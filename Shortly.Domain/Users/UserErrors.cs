using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Users;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "User was not found.");
    public static readonly Error InvalidCredentials = Error.Unauthorized("User.InvalidCredentials", "Email or password is incorrect.");
    public static readonly Error EmailNotUnique = Error.Conflict("User.EmailNotUnique", "An account with this email already exists.");
    public static readonly Error UsernameNotUnique = Error.Conflict("User.UsernameNotUnique", "This username is already taken.");
    public static readonly Error EmailOrUsernameNotUnique = Error.Conflict("User.EmailOrUsernameNotUnique", "Email or username is already taken.");
    public static readonly Error EmailAlreadyConfirmed = Error.Conflict("User.EmailAlreadyConfirmed", "This email is already verified.");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized("User.InvalidRefreshToken", "Refresh token is missing, invalid or expired.");
}