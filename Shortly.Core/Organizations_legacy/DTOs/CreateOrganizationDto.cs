namespace Shortly.Core.Organizations_legacy.DTOs;

public record CreateOrganizationDto(string Name, string? Description, string? Website, string? LogoUrl, int MemberLimit);