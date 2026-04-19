using Shortly.Core.Tokens.DTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Tokens.Mappers;

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