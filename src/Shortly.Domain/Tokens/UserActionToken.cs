using Shortly.Domain.Abstractions;

namespace Shortly.Domain.Tokens;

public sealed class UserActionToken : Entity<Guid>
{
    private UserActionToken(Guid id, Guid userId, enUserActionTokenType tokenType, string tokenHash, DateTime expiresAt, string? newEmail, DateTime utcNow)
        : base(id)
    {
        UserId = userId;
        TokenType = tokenType;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        NewEmail = newEmail;
        CreatedAt = utcNow;
    }

    private UserActionToken() { } // EF

    public Guid UserId { get; private set; }
    public enUserActionTokenType TokenType { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool Used { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public string? NewEmail { get; private set; } // only set when TokenType == EmailChange

    public static Result<UserActionToken> IssueEmailVerification(Guid userId, string tokenHash, DateTime utcNow)
    {
        var check = ValidateIssue(userId, tokenHash);
        if (check.IsFailure) return Result.Failure<UserActionToken>(check.Error);

        return new UserActionToken(Guid.NewGuid(), userId, enUserActionTokenType.EmailVerification, tokenHash, utcNow.AddHours(24), null, utcNow);
    }

    public static Result<UserActionToken> IssuePasswordReset(Guid userId, string tokenHash, DateTime utcNow)
    {
        var check = ValidateIssue(userId, tokenHash);
        if (check.IsFailure) return Result.Failure<UserActionToken>(check.Error);

        return new UserActionToken(Guid.NewGuid(), userId, enUserActionTokenType.PasswordReset, tokenHash, utcNow.AddHours(1), null, utcNow);
    }

    public static Result<UserActionToken> IssueEmailChange(Guid userId, string tokenHash, string newEmail, DateTime utcNow)
    {
        var check = ValidateIssue(userId, tokenHash);
        if (check.IsFailure) return Result.Failure<UserActionToken>(check.Error);

        if (string.IsNullOrWhiteSpace(newEmail))
            return Result.Failure<UserActionToken>(UserActionTokenErrors.NewEmailRequired);

        return new UserActionToken(Guid.NewGuid(), userId, enUserActionTokenType.EmailChange, tokenHash, utcNow.AddHours(1), newEmail, utcNow);
    }

    /// <summary>
    /// Checks type/used/expiry in one place instead of each consuming handler
    /// repeating the same three-part if-chain. Doesn't mutate anything — call
    /// MarkUsed() (or the repository's bulk ConsumeAsync) separately once this passes.
    /// </summary>
    public Result EnsureValidFor(enUserActionTokenType expectedType, DateTime utcNow)
    {
        if (TokenType != expectedType) return Result.Failure(UserActionTokenErrors.WrongType);
        if (Used) return Result.Failure(UserActionTokenErrors.Used);
        if (ExpiresAt <= utcNow) return Result.Failure(UserActionTokenErrors.Expired);
        return Result.Success();
    }

    public Result MarkUsed(DateTime utcNow)
    {
        if (Used) return Result.Failure(UserActionTokenErrors.Used);

        Used = true;
        UsedAt = utcNow;
        return Result.Success();
    }

    private static Result ValidateIssue(Guid userId, string tokenHash)
    {
        if (userId == Guid.Empty) return Result.Failure(UserActionTokenErrors.UserRequired);
        if (string.IsNullOrWhiteSpace(tokenHash)) return Result.Failure(UserActionTokenErrors.TokenHashRequired);
        return Result.Success();
    }
}