using Shortly.Domain.Enums;

namespace Shortly.Core.Tokens.DTOs;

public record UserActionTokenDto(
    Guid Id,
    Guid UserId,
    enUserActionTokenType TokenType,
    string PlainToken,
    DateTime ExpiresAt,
    bool Used,
    DateTime CreatedAt
);