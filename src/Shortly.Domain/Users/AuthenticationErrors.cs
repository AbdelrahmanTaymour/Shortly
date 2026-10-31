using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Users;

public static class AuthenticationErrors
{
    public static readonly Error NotAuthenticated = Error.Unauthorized(
        "Authentication.NotAuthenticated", "You must be logged in to perform this action.");
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Authentication.InvalidCredentials", "Email or password is incorrect.");
    public static readonly Error EmailNotUnique = Error.Conflict(
        "Authentication.EmailNotUnique", "An account with this email already exists.");
    public static readonly Error UsernameNotUnique = Error.Conflict(
        "Authentication.UsernameNotUnique", "This username is already taken.");
    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Authentication.InvalidRefreshToken", "Refresh token is invalid or expired.");
}
