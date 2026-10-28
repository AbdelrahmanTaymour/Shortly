using Shortly.Domain.Abstractions;

namespace Shortly.Domain.ShortUrls;

public static class ShortUrlErrors
{
    public static readonly Error ShortCodeEmpty = Error.Validation("ShortUrl.ShortCodeEmpty", "Short code cannot be empty.");
    public static readonly Error ShortCodeInvalidLength = Error.Validation("ShortUrl.ShortCodeInvalidLength", $"Short code must be between {ShortCode.MinLength} and {ShortCode.MaxLength} characters.");
    public static readonly Error ShortCodeInvalidCharacters = Error.Validation("ShortUrl.ShortCodeInvalidCharacters", "Short code may only contain letters, digits, hyphens and underscores.");
    public static readonly Error ShortCodeAlreadyExists = Error.Conflict("ShortUrl.ShortCodeAlreadyExists", "This short code is already in use.");
    
    public static readonly Error OriginalUrlEmpty = Error.Validation("ShortUrl.OriginalUrlEmpty", "URL cannot be empty.");
    public static readonly Error OriginalUrlTooLong = Error.Validation("ShortUrl.OriginalUrlTooLong", "URL is too long.");
    public static readonly Error OriginalUrlInvalid = Error.Validation("ShortUrl.OriginalUrlInvalid", "URL must be a valid absolute http or https address.");

    public static readonly Error NotFound = Error.NotFound("ShortUrl.NotFound", "Short URL was not found.");
    public static readonly Error AlreadyActive = Error.Conflict("ShortUrl.AlreadyActive", "Short URL is already active.");
    public static readonly Error AlreadyInactive = Error.Conflict("ShortUrl.AlreadyInactive", "Short URL is already inactive.");
    public static readonly Error NotPasswordProtected = Error.Conflict("ShortUrl.NotPasswordProtected", "Short URL is not password protected.");
    public static readonly Error InvalidPassword = Error.Unauthorized("ShortUrl.InvalidPassword", "The password provided is incorrect or missing.");
    public static readonly Error PasswordRequired = Error.Unauthorized("ShortUrl.PasswordRequired", "This short URL requires a password.");
    public static readonly Error InvalidClickLimit = Error.Validation("ShortUrl.InvalidClickLimit", "Click limit must be greater than zero, or -1 for unlimited.");
    public static readonly Error InvalidExpiration = Error.Validation("ShortUrl.InvalidExpiration", "Expiration date must be in the future.");
    public static readonly Error Expired = Error.Forbidden("ShortUrl.Expired", "Short URL has expired.");
    public static readonly Error ClickLimitReached = Error.Forbidden("ShortUrl.ClickLimitReached", "Short URL has reached its click limit.");
    public static readonly Error Inactive = Error.Forbidden("ShortUrl.Inactive", "Short URL is inactive.");
    public static readonly Error InvalidOwnerType = Error.Failure("ShortUrl.InvalidOwnerType", "The current context does not have a valid owner type for creating a short URL.");}