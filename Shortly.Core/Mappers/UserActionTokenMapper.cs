using Shortly.Core.DTOs.UserActionTokensDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class UserActionTokenMapper
{
    public static UserActionTokenDto MapToUserActionTokenDto(this UserActionToken token, string plainToken)
    {
        return new UserActionTokenDto(
            token.Id,
            token.UserId,
            token.TokenType,
            plainToken,
            token.ExpiresAt,
            token.Used,
            token.CreatedAt
        );
    }
}