namespace Shortly.Core.Tokens.DTOs;

public record EmailChangeTokenDto(
    Guid Id,
    string Token,
    Guid UserId,
    string OldEmail,
    string NewEmail,
    DateTime ExpiresAt,
    bool IsUsed,
    DateTime CreatedAt,
    DateTime? UsedAt
);