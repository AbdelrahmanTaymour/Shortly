using Shortly.Core.DTOs.RefreshTokenDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

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