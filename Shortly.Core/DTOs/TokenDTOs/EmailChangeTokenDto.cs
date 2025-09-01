namespace Shortly.Core.DTOs.TokenDTOs;

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