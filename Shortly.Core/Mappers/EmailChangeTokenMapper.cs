using Shortly.Core.DTOs.TokenDTOs;
using Shortly.Domain.Entities;

namespace Shortly.Core.Mappers;

public static class EmailChangeTokenMapper
{
    public static EmailChangeTokenDto MapToEmailChangeTokenDto(this EmailChangeToken token)
    {
        return new EmailChangeTokenDto(
            token.Id,
            token.Token,
            token.UserId,
            token.OldEmail,
            token.NewEmail,
            token.ExpiresAt,
            token.IsUsed,
            token.CreatedAt,
            token.UsedAt
        );
    }

    public static IEnumerable<EmailChangeTokenDto> MapToEmailChangeTokenDtos(this IEnumerable<EmailChangeToken> tokens)
    {
        return tokens.Select(MapToEmailChangeTokenDto);
    }
}