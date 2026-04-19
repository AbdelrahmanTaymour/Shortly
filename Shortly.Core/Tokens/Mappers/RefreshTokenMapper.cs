using Shortly.Core.Tokens.DTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Tokens.Mappers;

public static class RefreshTokenMapper
{
    public static RefreshTokenDto MapToRefreshTokenDto(this RefreshToken refreshToken, string unHasedToken)
    {
        return new RefreshTokenDto(
            refreshToken.Id,
            refreshToken.UserId,
            unHasedToken,
            refreshToken.ExpiresAt,
            refreshToken.IsRevoked,
            refreshToken.RevokedAt,
            refreshToken.CreatedAt,
            refreshToken.UpdatedAt
        );
    }
}