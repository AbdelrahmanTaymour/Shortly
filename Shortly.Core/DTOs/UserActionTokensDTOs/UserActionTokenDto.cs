using Shortly.Domain.Enums;

namespace Shortly.Core.DTOs.UserActionTokensDTOs;

public record UserActionTokenDto(
    Guid Id,
    Guid UserId,
    enUserActionTokenType TokenType,
    string PlainToken,
    DateTime ExpiresAt,
    bool Used,
    DateTime CreatedAt
);