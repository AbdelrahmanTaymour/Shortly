namespace Shortly.Core.Organizations.DTOs;

public record OrganizationDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string? Description,
    string? Website,
    string? LogoUrl,
    int MemberLimit,
    bool IsActive,
    bool IsSubscribed,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);