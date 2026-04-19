namespace Shortly.Core.Tokens.DTOs;

public record RefreshTokenDto(
    Guid Id,
    Guid UserId,
    string Token,
    DateTime ExpiresAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);