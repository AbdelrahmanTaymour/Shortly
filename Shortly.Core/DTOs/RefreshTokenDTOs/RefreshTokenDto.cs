namespace Shortly.Core.DTOs.RefreshTokenDTOs;

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