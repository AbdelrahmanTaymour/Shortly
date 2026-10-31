using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Tokens;

public static class UserActionTokenErrors
{
    public static readonly Error NotFound = Error.NotFound("UserActionToken.NotFound", "Token is invalid.");
    public static readonly Error WrongType = Error.Validation("UserActionToken.WrongType", "Token type mismatch.");
    public static readonly Error Used = Error.Conflict("UserActionToken.Used", "Token has already been used.");
    public static readonly Error Expired = Error.Forbidden("UserActionToken.Expired", "Token has expired.");
    public static readonly Error NewEmailRequired = Error.Validation("UserActionToken.NewEmailRequired", "A new email address is required.");
}